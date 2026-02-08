using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record GetTripByIdQueryRequest(Guid TripId);

public sealed record GetTripByIdQueryResponse(TripQueryModel Trip, IReadOnlyList<TripTagQueryModel> Tags);
