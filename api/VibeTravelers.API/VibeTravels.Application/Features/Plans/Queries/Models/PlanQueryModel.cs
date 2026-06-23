using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Plans.Queries.Models;

public sealed record PlanQueryModel(
    Guid TripId,
    int Version,
    PlanStatus Status,
    Guid? GeneratedFromJobId,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? SavedAt,
    string? Summary,
    IReadOnlyList<PlanItemQueryModel> Items);
