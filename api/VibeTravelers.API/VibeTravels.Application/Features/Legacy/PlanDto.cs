using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Plans.Models;

public sealed record PlanDto(
    Guid TripId,
    int Version,
    PlanStatus Status,
    Guid? GeneratedFromJobId,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? SavedAt,
    string? Summary,
    IReadOnlyList<PlanItemDto> Items);
