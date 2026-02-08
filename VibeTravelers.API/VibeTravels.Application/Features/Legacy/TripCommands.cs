using System;
using VibeTravels.Application.Features.Legacy.Trips;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record CreateTripCommand(CreateTripRequest Request);

public sealed record UpdateTripCommand(Guid TripId, UpdateTripRequest Request);

public sealed record DeleteTripCommand(Guid TripId);
