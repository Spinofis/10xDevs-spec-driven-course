using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VibeTravels.Application.Abstractions.Integrations;
using VibeTravels.Application.Features.Jobs.Models;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Infrastructure.Persistence;
using VibeTravels.Worker.Options;

namespace VibeTravels.Worker.Processing;

public sealed class GenerationJobProcessor
{
    private readonly AppDbContext _db;
    private readonly IOpenAiClient _openAiClient;
    private readonly IOptionsMonitor<GenerationWorkerOptions> _optionsMonitor;
    private readonly ILogger<GenerationJobProcessor> _logger;

    public GenerationJobProcessor(
        AppDbContext db,
        IOpenAiClient openAiClient,
        IOptionsMonitor<GenerationWorkerOptions> optionsMonitor,
        ILogger<GenerationJobProcessor> logger)
    {
        _db = db;
        _openAiClient = openAiClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var workerOptions = _optionsMonitor.CurrentValue;
        _db.Database.SetCommandTimeout(TimeSpan.FromSeconds(Math.Max(1, workerOptions.CommandTimeoutSeconds)));

        var job = await _db.AiGenerationJobs
            .SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        if (job is null || job.Status != AiGenerationJobStatus.Running)
            return;

        if (GenerationJobRequestPayload.TryFromJson(job.InputSnapshot, out var payload) is false || payload is null)
        {
            await MarkFailedAsync(
                job,
                GenerationJobErrorCodes.JobPayloadInvalid,
                "Generation job payload is invalid JSON.",
                cancellationToken);
            return;
        }

        var requestResult = TryBuildOpenAiRequest(payload, out var openAiRequest, out var payloadErrorMessage);
        if (requestResult is false || openAiRequest is null)
        {
            await MarkFailedAsync(
                job,
                GenerationJobErrorCodes.JobPayloadInvalid,
                payloadErrorMessage,
                cancellationToken);
            return;
        }

        var openAiResult = await _openAiClient.GenerateTripPlanAsync(openAiRequest, cancellationToken);
        if (openAiResult.IsSuccess is false || openAiResult.Result is null)
        {
            await HandleOpenAiFailureAsync(job, openAiResult, cancellationToken);
            return;
        }

        var validationResult = ValidateGeneratedResult(openAiResult.Result, payload, out var validationError);
        if (validationResult is false)
        {
            await MarkFailedAsync(
                job,
                GenerationJobErrorCodes.OpenAiInvalidResponse,
                validationError,
                cancellationToken);
            return;
        }

        await PersistSuccessfulResultAsync(job.Id, payload, openAiResult.Result, cancellationToken);
    }

    private async Task PersistSuccessfulResultAsync(
        Guid jobId,
        GenerationJobRequestPayload payload,
        TripPlanGenerationResult generatedResult,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var job = await _db.AiGenerationJobs.SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null || job.Status != AiGenerationJobStatus.Running)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        job.SetResponsePayload(generatedResult.RawResponsePayload);

        var newerJobExists = await _db.AiGenerationJobs
            .AsNoTracking()
            .AnyAsync(
                x => x.TripId == job.TripId
                     && x.Id != job.Id
                     && x.RequestedAt > job.RequestedAt
                     && (x.Status == AiGenerationJobStatus.Pending
                         || x.Status == AiGenerationJobStatus.Running
                         || x.Status == AiGenerationJobStatus.Succeeded),
                cancellationToken);

        if (newerJobExists)
        {
            job.MarkDiscardedAsSucceeded(now, "newer_job_exists");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var trip = await _db.Trips.SingleOrDefaultAsync(x => x.Id == job.TripId, cancellationToken);
        if (trip is null)
        {
            job.MarkFailed(now, GenerationJobErrorCodes.PlanPersistFailed, "Trip not found during plan persist.");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var existingPlan = await _db.TripPlans
            .SingleOrDefaultAsync(x => x.TripId == job.TripId, cancellationToken);

        if (existingPlan is null)
        {
            _db.TripPlans.Add(TripPlan.Create(
                tripId: job.TripId,
                generationJobId: job.Id,
                title: payload.Title,
                summary: generatedResult.Summary,
                createdAt: now));
        }
        else
        {
            existingPlan.UpdateGenerated(
                generationJobId: job.Id,
                title: payload.Title,
                summary: generatedResult.Summary,
                updatedAt: now);
        }

        var existingItems = await _db.PlanItems
            .Where(x => x.TripId == job.TripId)
            .ToListAsync(cancellationToken);
        if (existingItems.Count > 0)
            _db.PlanItems.RemoveRange(existingItems);

        var generatedItems = generatedResult.Days
            .OrderBy(x => x.Date)
            .Select((day, index) => new { Day = day, DayNumber = index + 1 })
            .SelectMany(x => x.Day.Items.Select(item => PlanItem.CreateGenerated(
                tripId: job.TripId,
                dayNumber: x.DayNumber,
                itemDate: new DateTimeOffset(
                    x.Day.Date.Year,
                    x.Day.Date.Month,
                    x.Day.Date.Day,
                    item.Time?.Hour ?? 0,
                    item.Time?.Minute ?? 0,
                    0,
                    TimeSpan.Zero),
                sortOrder: item.Order,
                placeType: ParsePlaceType(item.PlaceType),
                title: item.PlaceName.Trim(),
                description: item.Description,
                locationText: item.PlaceName.Trim(),
                createdAt: now)))
            .ToArray();

        _db.PlanItems.AddRange(generatedItems);

        trip.MarkPlanGenerated(now);
        job.MarkSucceeded(now);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleOpenAiFailureAsync(
        AiGenerationJob job,
        OpenAiClientResult openAiResult,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _optionsMonitor.CurrentValue.MaxAttempts);
        var code = openAiResult.ErrorCode ?? GenerationJobErrorCodes.OpenAiHttpError;
        var message = openAiResult.ErrorMessage;

        if (openAiResult.IsTransient && job.AttemptNo < maxAttempts)
        {
            job.MarkPendingForRetry(code, message);
        }
        else
        {
            job.MarkFailed(DateTimeOffset.UtcNow, code, message);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(
        AiGenerationJob job,
        string errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        job.MarkFailed(DateTimeOffset.UtcNow, errorCode, errorMessage);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryBuildOpenAiRequest(
        GenerationJobRequestPayload payload,
        out TripPlanGenerationRequest? request,
        out string errorMessage)
    {
        if (payload.DateFrom is null || payload.DateTo is null)
        {
            request = null;
            errorMessage = "Date range is required in generation payload.";
            return false;
        }

        if (payload.StayLengthMinDays is null || payload.StayLengthMaxDays is null)
        {
            request = null;
            errorMessage = "Stay length range is required in generation payload.";
            return false;
        }

        if (payload.PeopleCount is null)
        {
            request = null;
            errorMessage = "PeopleCount is required in generation payload.";
            return false;
        }

        request = new TripPlanGenerationRequest(
            payload.TripId,
            payload.UserId,
            payload.Title,
            payload.PlaceText,
            payload.NoteText,
            payload.DateFrom.Value,
            payload.DateTo.Value,
            payload.StayLengthMinDays.Value,
            payload.StayLengthMaxDays.Value,
            payload.PeopleCount.Value,
            payload.BudgetLevel,
            payload.Pace,
            payload.Tags.Select(tag => new TripPlanGenerationTag(
                tag.TagId,
                tag.Code,
                tag.DisplayName,
                tag.Order)).ToArray());

        errorMessage = string.Empty;
        return true;
    }

    private static bool ValidateGeneratedResult(
        TripPlanGenerationResult result,
        GenerationJobRequestPayload payload,
        out string? errorMessage)
    {
        if (result.Days.Count == 0)
        {
            errorMessage = "Generated plan has no days.";
            return false;
        }

        if (payload.DateFrom is null || payload.DateTo is null)
        {
            errorMessage = "Payload is missing date range.";
            return false;
        }

        var minDate = payload.DateFrom.Value;
        var maxDate = payload.DateTo.Value;

        if (result.Days.Any(day => day.Date < minDate || day.Date > maxDate))
        {
            errorMessage = "Generated day date is outside requested date range.";
            return false;
        }

        var distinctDayCount = result.Days.Select(day => day.Date).Distinct().Count();
        if (distinctDayCount != result.Days.Count)
        {
            errorMessage = "Generated plan contains duplicate dates.";
            return false;
        }

        if (payload.StayLengthMinDays is not null && result.Days.Count < payload.StayLengthMinDays.Value)
        {
            errorMessage = "Generated day count is lower than requested minimum stay length.";
            return false;
        }

        if (payload.StayLengthMaxDays is not null && result.Days.Count > payload.StayLengthMaxDays.Value)
        {
            errorMessage = "Generated day count is greater than requested maximum stay length.";
            return false;
        }

        foreach (var day in result.Days)
        {
            if (day.Items.Count == 0)
            {
                errorMessage = "Generated plan day contains no items.";
                return false;
            }

            if (day.Items.Any(item => string.IsNullOrWhiteSpace(item.PlaceName)))
            {
                errorMessage = "Generated plan item has empty place name.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    private static PlanItemPlaceType ParsePlaceType(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return PlanItemPlaceType.Attraction;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "restaurant" => PlanItemPlaceType.Restaurant,
            "hotel" => PlanItemPlaceType.Hotel,
            _ => PlanItemPlaceType.Attraction
        };
    }
}
