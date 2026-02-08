using System.Collections.Generic;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Plans.Models;

public sealed record PlanItemInputDto(
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
