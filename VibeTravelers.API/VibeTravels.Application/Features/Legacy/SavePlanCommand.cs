using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Plans.Commands;

public sealed record SavePlanCommandRequest(Guid TripId, int? IfMatchVersion);

public sealed record SavePlanCommandResponse(
    Guid TripId,
    PlanStatus Status,
    DateTimeOffset SavedAt,
    int Version);
