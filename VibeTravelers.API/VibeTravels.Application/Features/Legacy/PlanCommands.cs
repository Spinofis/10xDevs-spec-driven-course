using System;
using VibeTravels.Application.Features.Legacy.Plans;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record ReplaceTripPlanCommand(Guid TripId, ReplacePlanRequest Request);

public sealed record SaveTripPlanCommand(Guid TripId, int? IfMatchVersion);
