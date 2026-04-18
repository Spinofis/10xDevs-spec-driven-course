using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Queries;
using VibeTravels.Application.Features.Plans.Services;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Plans.Handlers;

public sealed class GetPlanByTripIdQueryHandler : IRequestHandler<GetPlanByTripIdQuery, Result<GetPlanByTripIdQueryResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ITripPlanReadService _tripPlanReadService;

    public GetPlanByTripIdQueryHandler(
        IAppDbContext db,
        ITripPlanReadService tripPlanReadService)
    {
        _db = db;
        _tripPlanReadService = tripPlanReadService;
    }

    public async Task<Result<GetPlanByTripIdQueryResponse>> Handle(
        GetPlanByTripIdQuery request,
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
            return Result<GetPlanByTripIdQueryResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var plan = await _tripPlanReadService.GetByTripIdAsync(request.Request.TripId, cancellationToken);
        if (plan is null)
            return Result<GetPlanByTripIdQueryResponse>.Fail(ResultErrors.PlanNotFound(nameof(request.Request.TripId)));

        return Result<GetPlanByTripIdQueryResponse>.Ok(new GetPlanByTripIdQueryResponse(plan));
    }
}
