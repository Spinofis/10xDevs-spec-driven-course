using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Queries.Models;

public sealed record PlanItemQueryModel(
    Guid Id,
    int DayNumber,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    TimeOnly? StartTime,
    PlanItemPlaceType PlaceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
