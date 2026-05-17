using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record SavePlanCommandRequest(Guid TripId);

public sealed record SavePlanCommandResponse(SavePlanResultQueryModel Result);
