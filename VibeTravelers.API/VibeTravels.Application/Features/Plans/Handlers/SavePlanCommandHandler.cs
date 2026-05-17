using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Queries.Models;
using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Plans.Handlers;

public sealed class SavePlanCommandHandler : IRequestHandler<SavePlanCommand, Result<SavePlanCommandResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ITripInputFingerprintService _tripInputFingerprintService;

    public SavePlanCommandHandler(
        IAppDbContext db,
        ITripInputFingerprintService tripInputFingerprintService)
    {
        _db = db;
        _tripInputFingerprintService = tripInputFingerprintService;
    }

    public async Task<Result<SavePlanCommandResponse>> Handle(
        SavePlanCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .Include(x => x.TripTags)
                .ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(
                x => x.Id == request.Request.TripId
                     && x.UserId == request.UserId
                     && x.DeletedAt == null,
                cancellationToken);

        if (trip is null)
            return Result<SavePlanCommandResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var plan = await _db.TripPlans
            .SingleOrDefaultAsync(x => x.TripId == request.Request.TripId, cancellationToken);

        if (plan is null)
            return Result<SavePlanCommandResponse>.Fail(ResultErrors.PlanNotFound(nameof(request.Request.TripId)));

        if (plan.GenerationJobId is Guid generationJobId)
        {
            var generationInputHash = await _db.AiGenerationJobs
                .AsNoTracking()
                .Where(x => x.Id == generationJobId)
                .Select(x => x.InputHash)
                .SingleOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(generationInputHash))
            {
                return Result<SavePlanCommandResponse>.Fail(
                    ResultErrors.InputChangedSinceGeneration(nameof(plan.GenerationJobId)));
            }

            var fingerprintResult = _tripInputFingerprintService.Build(trip, request.UserId);
            if (fingerprintResult.IsSuccess is false || fingerprintResult.Value is null)
                return Result<SavePlanCommandResponse>.Fail(fingerprintResult.Errors);

            if (string.Equals(
                    generationInputHash,
                    fingerprintResult.Value.Hash,
                    StringComparison.Ordinal) is false)
            {
                return Result<SavePlanCommandResponse>.Fail(
                    ResultErrors.InputChangedSinceGeneration(nameof(plan.GenerationJobId)));
            }
        }

        var now = DateTimeOffset.UtcNow;
        plan.Save(now);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new SavePlanCommandResponse(
            new SavePlanResultQueryModel(
                plan.TripId,
                PlanStatus.Saved,
                now,
                plan.Version));

        return Result<SavePlanCommandResponse>.Ok(response);
    }
}
