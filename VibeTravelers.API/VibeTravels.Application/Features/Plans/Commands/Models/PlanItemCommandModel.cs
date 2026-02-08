using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Plans.Commands.Models;

public sealed record PlanItemCommandModel(
    int DayNumber,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? DurationMinutes,
    CostLevel? CostLevel,
    IReadOnlyList<string> Tags);
