using System.Collections.Generic;
using VibeTravels.Application.Features.Legacy.Me.Models;

namespace VibeTravels.Application.Features.Legacy.Me.Commands;

public sealed record UpsertPreferenceTagsCommandRequest(IReadOnlyList<PreferenceTagInputDto> Items);

public sealed record UpsertPreferenceTagsCommandResponse(IReadOnlyList<PreferenceTagDto> Items);
