using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Jobs.Commands;
using VibeTravels.Application.Features.Jobs.Queries.Models;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Jobs;

namespace VibeTravels.Application.Features.Jobs.Handlers;

public sealed class QueueGenerationJobCommandHandler : IRequestHandler<QueueGenerationJobCommand, Result<QueueGenerationJobCommandResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IGenerationJobStatusMapper _generationJobStatusMapper;
    private readonly ITripInputFingerprintService _tripInputFingerprintService;

    public QueueGenerationJobCommandHandler(
        IAppDbContext db,
        IGenerationJobStatusMapper generationJobStatusMapper,
        ITripInputFingerprintService tripInputFingerprintService)
    {
        _db = db;
        _generationJobStatusMapper = generationJobStatusMapper;
        _tripInputFingerprintService = tripInputFingerprintService;
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

        var fingerprintResult = _tripInputFingerprintService.Build(trip, request.UserId);
        if (fingerprintResult.IsSuccess is false || fingerprintResult.Value is null)
            return Result<QueueGenerationJobCommandResponse>.Fail(fingerprintResult.Errors);

        var payloadJson = fingerprintResult.Value.PayloadJson;
        var inputHash = fingerprintResult.Value.Hash;
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

}
