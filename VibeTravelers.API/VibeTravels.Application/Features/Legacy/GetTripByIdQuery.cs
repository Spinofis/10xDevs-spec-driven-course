using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Legacy.Trips.Models;

namespace VibeTravels.Application.Features.Legacy.Trips.Queries;

public sealed record GetTripByIdQueryRequest(Guid TripId);

public sealed record GetTripByIdQueryResponse(TripDto Trip, IReadOnlyList<TripTagDto> Tags);
