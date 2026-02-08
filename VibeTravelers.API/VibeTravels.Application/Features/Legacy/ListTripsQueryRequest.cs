using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Trips.Queries;

public sealed record ListTripsQueryRequest(
    string? Query,
    bool? HasPlan,
    int? Limit,
    string? Cursor,
    string? Sort)
    : IPagedRequest, ISortableRequest;
