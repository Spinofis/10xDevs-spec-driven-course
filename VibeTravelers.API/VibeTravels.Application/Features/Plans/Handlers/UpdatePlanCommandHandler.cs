using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Services;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Jobs;

namespace VibeTravels.Application.Features.Plans.Handlers;

public sealed class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, Result<UpdatePlanCommandResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ITripPlanReadService _tripPlanReadService;
    private readonly ITripPlanWriteService _tripPlanWriteService;
    
    public UpdatePlanCommandHandler(
        IAppDbContext db,
        ITripPlanReadService tripPlanReadService,
        ITripPlanWriteService tripPlanWriteService)
    {
        _db = db;
        _tripPlanReadService = tripPlanReadService;
        _tripPlanWriteService = tripPlanWriteService;
    }

    public async Task<Result<UpdatePlanCommandResponse>> Handle(
        UpdatePlanCommand request,
        CancellationToken cancellationToken)
    {
        var tripExists = await _db.Trips
            .AsNoTracking()
            .AnyAsync(
                t => t.Id == request.Request.TripId
                     && t.UserId == request.UserId
                     && t.DeletedAt == null,
                cancellationToken);

        if (tripExists is false)
            return Result<UpdatePlanCommandResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var hasActiveGenerationJob = await _db.AiGenerationJobs
            .AsNoTracking()
            .AnyAsync(
                x => x.TripId == request.Request.TripId
                     && x.Discarded == false
                     && (x.Status == AiGenerationJobStatus.Pending || x.Status == AiGenerationJobStatus.Running),
                cancellationToken);

        if (hasActiveGenerationJob)
            return Result<UpdatePlanCommandResponse>.Fail(ResultErrors.JobAlreadyActive(nameof(request.Request.TripId)));

        var plan = await _db.TripPlans
            .SingleOrDefaultAsync(x => x.TripId == request.Request.TripId, cancellationToken);

        if (plan is null)
            return Result<UpdatePlanCommandResponse>.Fail(ResultErrors.PlanNotFound(nameof(request.Request.TripId)));

        var now = DateTimeOffset.UtcNow;
        var normalizedSummary = string.IsNullOrWhiteSpace(request.Request.Summary)
            ? null
            : request.Request.Summary.Trim();

        plan.UpdateManual(normalizedSummary, now);
        await _tripPlanWriteService.ReplacePlanItemsAsync(plan, request.Request.Items, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var mappedPlan = await _tripPlanReadService.GetByTripIdAsync(request.Request.TripId, cancellationToken);
        if (mappedPlan is null)
            return Result<UpdatePlanCommandResponse>.Fail(ResultErrors.PlanNotFound(nameof(request.Request.TripId)));

        return Result<UpdatePlanCommandResponse>.Ok(new UpdatePlanCommandResponse(mappedPlan));
    }
}
