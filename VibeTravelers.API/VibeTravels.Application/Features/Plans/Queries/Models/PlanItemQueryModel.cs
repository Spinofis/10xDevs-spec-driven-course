using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Plans.Queries.Models;

public sealed record PlanItemQueryModel(
    Guid Id,
    int DayNumber,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? DurationMinutes,
    CostLevel? CostLevel,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
