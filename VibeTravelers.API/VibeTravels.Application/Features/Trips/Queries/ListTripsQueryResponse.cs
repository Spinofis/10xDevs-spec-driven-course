using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record ListTripsQueryResponse(IReadOnlyList<TripQueryModel> Items, string? NextCursor)
    : PagedResponse<TripQueryModel>(Items, NextCursor);
