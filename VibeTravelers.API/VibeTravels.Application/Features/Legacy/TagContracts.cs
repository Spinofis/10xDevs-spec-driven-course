using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Tags;

public sealed record TagDto(Guid Id, string Code, string DisplayName, DateTimeOffset CreatedAt);

public sealed record ListTagsRequest(int? Limit, string? Cursor, string? Sort)
    : IPagedRequest, ISortableRequest;

public sealed record ListTagsResponse(IReadOnlyList<TagDto> Items, string? NextCursor)
    : PagedResponse<TagDto>(Items, NextCursor);
