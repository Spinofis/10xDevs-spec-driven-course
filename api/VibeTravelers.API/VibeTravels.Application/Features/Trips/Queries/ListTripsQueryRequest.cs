using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record ListTripsQueryRequest(
    string? Query,
    bool? HasPlan,
    bool? IncludeDeleted,
    int? Limit,
    string? Cursor,
    string? Sort)
    : IPagedRequest, ISortableRequest;
