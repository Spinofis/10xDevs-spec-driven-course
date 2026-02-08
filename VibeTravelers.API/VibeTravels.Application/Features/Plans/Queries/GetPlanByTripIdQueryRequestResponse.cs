using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Queries;

public sealed record GetPlanByTripIdQueryRequest(Guid TripId);

public sealed record GetPlanByTripIdQueryResponse(PlanQueryModel Plan);
