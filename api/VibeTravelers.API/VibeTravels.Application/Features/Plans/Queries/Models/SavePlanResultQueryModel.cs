using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Plans.Queries.Models;

public sealed record SavePlanResultQueryModel(
    Guid TripId,
    PlanStatus Status,
    DateTimeOffset SavedAt,
    int Version);
