using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Commands.Models;

public sealed record PlanItemCommandModel(
    Guid Id,
    int DayNumber,
    DateTimeOffset ItemDate,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PlanItemPlaceType PlaceType);
