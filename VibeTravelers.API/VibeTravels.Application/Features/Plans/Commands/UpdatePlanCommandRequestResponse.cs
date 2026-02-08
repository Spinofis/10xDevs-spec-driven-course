using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record UpdatePlanCommandRequest(Guid TripId, UpdatePlanCommandModel Model);

public sealed record UpdatePlanCommandResponse(PlanQueryModel Plan);
