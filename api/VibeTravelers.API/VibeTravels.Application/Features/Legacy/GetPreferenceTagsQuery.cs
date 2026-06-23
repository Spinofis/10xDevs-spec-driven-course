using System.Collections.Generic;
using VibeTravels.Application.Features.Legacy.Me.Models;

namespace VibeTravels.Application.Features.Legacy.Me.Queries;

public sealed record GetPreferenceTagsQueryRequest;

public sealed record GetPreferenceTagsQueryResponse(IReadOnlyList<PreferenceTagDto> Items);
