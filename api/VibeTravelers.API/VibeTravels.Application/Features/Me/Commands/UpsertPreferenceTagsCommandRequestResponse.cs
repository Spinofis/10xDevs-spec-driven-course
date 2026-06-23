using System.Collections.Generic;
using VibeTravels.Application.Features.Me.Commands.Models;
using VibeTravels.Application.Features.Me.Queries.Models;

namespace VibeTravels.Application.Features.Me.Commands;

public sealed record UpsertPreferenceTagsCommandRequest(IReadOnlyList<PreferenceTagCommandModel> Items);

public sealed record UpsertPreferenceTagsCommandResponse(IReadOnlyList<PreferenceTagQueryModel> Items);
