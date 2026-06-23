using System.Collections.Generic;
using VibeTravels.Application.Features.Me.Commands.Models;
using VibeTravels.Application.Features.Me.Queries.Models;

namespace VibeTravels.Application.Features.Me.Commands;

public sealed record UpsertUserProfileCommandRequest(
    UserProfileCommandModel Profile,
    IReadOnlyList<PreferenceTagCommandModel> PreferenceTags);

public sealed record UpsertUserProfileCommandResponse(
    Guid UserId,
    UserProfileQueryModel Profile,
    IReadOnlyList<PreferenceTagQueryModel> PreferenceTags);
