using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Tags.Models;

namespace VibeTravels.Application.Features.Legacy.Tags.Queries;

public sealed record ListTagsQueryResponse(IReadOnlyList<TagDto> Items)
    : PagedResponse<TagDto>(Items);
