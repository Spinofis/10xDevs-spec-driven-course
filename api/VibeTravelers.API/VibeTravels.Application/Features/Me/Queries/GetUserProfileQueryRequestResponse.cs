namespace VibeTravels.Application.Features.Me.Queries;

public sealed record GetUserProfileQueryRequest;

public sealed record GetUserProfileQueryResponse(
    Guid UserId,
    Models.UserProfileQueryModel Profile,
    IReadOnlyList<Models.PreferenceTagQueryModel> PreferenceTags);
