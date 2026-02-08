using System;
using VibeTravels.Application.Features.Legacy.Plans.Models;

namespace VibeTravels.Application.Features.Legacy.Plans.Queries;

public sealed record GetPlanByTripIdQueryRequest(Guid TripId);

public sealed record GetPlanByTripIdQueryResponse(PlanDto Plan);
