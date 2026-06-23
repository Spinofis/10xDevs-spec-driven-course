using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Trips.Queries;

public sealed record ListTripInputSnapshotsQueryRequest(
    Guid TripId,
    int? Limit,
    string? Cursor,
    string? Sort)
    : IPagedRequest, ISortableRequest;
