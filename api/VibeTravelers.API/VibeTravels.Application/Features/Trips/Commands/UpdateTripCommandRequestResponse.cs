using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record UpdateTripCommandRequest(Guid TripId, UpdateTripCommandModel Model);

public sealed record UpdateTripCommandResponse(TripQueryModel Trip);
