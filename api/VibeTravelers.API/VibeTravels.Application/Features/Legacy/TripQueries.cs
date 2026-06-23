using System;
using VibeTravels.Application.Features.Legacy.Trips;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record ListTripsQuery(ListTripsRequest Request);

public sealed record GetTripQuery(Guid TripId);

public sealed record ListTripInputSnapshotsQuery(Guid TripId, ListTripInputSnapshotsRequest Request);
