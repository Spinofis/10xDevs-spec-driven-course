using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Tags.Queries.Models;

namespace VibeTravels.Application.Features.Tags.Queries;

public sealed record ListTagsQueryResponse(IReadOnlyList<TagQueryModel> Items, string? NextCursor)
    : PagedResponse<TagQueryModel>(Items, NextCursor);
