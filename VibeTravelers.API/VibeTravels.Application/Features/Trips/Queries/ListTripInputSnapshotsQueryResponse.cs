using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record ListTripInputSnapshotsQueryResponse(
    IReadOnlyList<TripInputSnapshotQueryModel> Items)
    : PagedResponse<TripInputSnapshotQueryModel>(Items);
