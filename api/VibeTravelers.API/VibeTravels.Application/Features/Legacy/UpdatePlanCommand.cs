using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Legacy.Plans.Models;

namespace VibeTravels.Application.Features.Legacy.Plans.Commands;

public sealed record UpdatePlanCommandRequest(
    Guid TripId,
    string? Summary,
    IReadOnlyList<PlanItemInputDto> Items);

public sealed record UpdatePlanCommandResponse(PlanDto Plan);
