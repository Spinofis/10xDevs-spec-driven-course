using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Tags.Queries;

public sealed record ListTagsQueryRequest(int? Limit, string? Cursor, string? Sort)
    : IPagedRequest, ISortableRequest;
