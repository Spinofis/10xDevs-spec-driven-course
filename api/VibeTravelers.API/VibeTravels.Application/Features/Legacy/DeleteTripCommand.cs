using System;

namespace VibeTravels.Application.Features.Legacy.Trips.Commands;

public sealed record DeleteTripCommandRequest(Guid TripId);

public sealed record DeleteTripCommandResponse;
