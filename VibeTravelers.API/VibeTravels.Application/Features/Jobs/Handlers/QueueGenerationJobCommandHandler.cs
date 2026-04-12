using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Jobs.Commands;
using VibeTravels.Application.Features.Jobs.Models;
using VibeTravels.Application.Features.Jobs.Queries.Models;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Jobs;

namespace VibeTravels.Application.Features.Jobs.Handlers;

public sealed class QueueGenerationJobCommandHandler : IRequestHandler<QueueGenerationJobCommand, Result<QueueGenerationJobCommandResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IGenerationJobStatusMapper _generationJobStatusMapper;

    public QueueGenerationJobCommandHandler(
        IAppDbContext db,
        IGenerationJobStatusMapper generationJobStatusMapper)
    {
        _db = db;
        _generationJobStatusMapper = generationJobStatusMapper;
    }

    public async Task<Result<QueueGenerationJobCommandResponse>> Handle(
        QueueGenerationJobCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .Include(t => t.TripTags)
                .ThenInclude(t => t.Tag)
            .SingleOrDefaultAsync(
                t => t.Id == request.Request.TripId
                     && t.UserId == request.UserId
                     && t.DeletedAt == null,
                cancellationToken);

        if (trip is null)
            return Result<QueueGenerationJobCommandResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var validateResult = ValidateTripForQueue(trip);
        if (validateResult.IsSuccess is false)
            return Result<QueueGenerationJobCommandResponse>.Fail(validateResult.Errors);

        var activeJobExists = await _db.AiGenerationJobs
            .AsNoTracking()
            .AnyAsync(
                x => x.TripId == request.Request.TripId
                     && (x.Status == AiGenerationJobStatus.Pending || x.Status == AiGenerationJobStatus.Running),
                cancellationToken);

        if (activeJobExists)
            return Result<QueueGenerationJobCommandResponse>.Fail(ResultErrors.JobAlreadyActive(nameof(request.Request.TripId)));

        var payloadResult = BuildPayloadJson(trip, request.UserId);
        if (payloadResult.IsSuccess is false || payloadResult.Value is null)
            return Result<QueueGenerationJobCommandResponse>.Fail(payloadResult.Errors);

        var payloadJson = payloadResult.Value;
        var inputHash = ComputeInputHash(payloadJson);
        var now = DateTimeOffset.UtcNow;

        var jobResult = AiGenerationJob.CreatePending(
            trip.Id,
            request.UserId,
            payloadJson,
            inputHash,
            now);

        if (jobResult.IsSuccess is false || jobResult.Value is null)
            return Result<QueueGenerationJobCommandResponse>.Fail(jobResult.Errors);

        _db.AiGenerationJobs.Add(jobResult.Value);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsOneActiveJobViolation(exception))
        {
            return Result<QueueGenerationJobCommandResponse>.Fail(ResultErrors.JobAlreadyActive(nameof(request.Request.TripId)));
        }

        var response = new QueueGenerationJobCommandResponse(
            new QueuedGenerationJobQueryModel(
                jobResult.Value.Id,
                jobResult.Value.TripId,
                _generationJobStatusMapper.Map(jobResult.Value.Status),
                jobResult.Value.RequestedAt,
                0));

        return Result<QueueGenerationJobCommandResponse>.Ok(response);
    }

    private static bool IsOneActiveJobViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
            return false;

        return postgresException.SqlState == PostgresErrorCodes.UniqueViolation
               && postgresException.ConstraintName == "generation_jobs_one_active_per_trip_ux";
    }

    private static Result ValidateTripForQueue(Domain.Entities.Trips.Trip trip)
    {
        var domainValidation = trip.ValidateForGenerationQueue(hasAtLeastTwoTags: trip.TripTags.Count >= 2);

        if (domainValidation.IsSuccess is false)
        {
            var message = string.Join("; ", domainValidation.Errors.Select(x => x.Message));
            return Result.Fail(ResultErrors.GenerationRequirementsNotMet(message, nameof(trip.Id)));
        }

        return Result.Ok();
    }

    private static Result<string> BuildPayloadJson(Domain.Entities.Trips.Trip trip, Guid userId)
    {
        var orderedTags = trip.TripTags
            .OrderBy(x => x.Order ?? 0)
            .ThenBy(x => x.TagId)
            .Select(x => new GenerationJobRequestPayloadTag(
                x.TagId,
                x.Tag.Code,
                x.Tag.DisplayName,
                x.Order ?? 0))
            .ToArray();

        var payload = new GenerationJobRequestPayload(
            trip.Id,
            userId,
            trip.Title.Value,
            trip.PlaceText?.Value,
            trip.NoteText,
            trip.DateFrom,
            trip.DateTo,
            trip.StayLengthMinDays,
            trip.StayLengthMaxDays,
            trip.PeopleCount,
            trip.BudgetLevel,
            trip.Pace,
            orderedTags);

        try
        {
            return Result<string>.Ok(payload.ToJson());
        }
        catch (Exception exception)
        {
            return Result<string>.Fail(ResultErrors.Unknown($"Failed to serialize generation payload: {exception.Message}"));
        }
    }

    private static string ComputeInputHash(string payloadJson)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
