using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Jobs.Queries;

public sealed record ListTripGenerationJobsQueryRequest(
    Guid TripId,
    int? Limit,
    string? Cursor,
    string? Sort)
    : IPagedRequest, ISortableRequest;
