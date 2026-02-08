using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record CreateTripCommandRequest(CreateTripCommandModel Model);

public sealed record CreateTripCommandResponse(TripQueryModel Trip, IReadOnlyList<TripTagQueryModel> Tags);
