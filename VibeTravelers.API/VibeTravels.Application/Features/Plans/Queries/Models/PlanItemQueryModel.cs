using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Queries.Models;

public sealed record PlanItemQueryModel(
    Guid Id,
    int DayNumber,
    int Order,
    string Title,
    DateTimeOffset ItemDate,
    string? Description,
    string? LocationText,
    PlanItemPlaceType PlaceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
