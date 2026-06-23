namespace VibeTravels.Application.Features.Me.Queries;

public sealed record GetPreferenceTagsQueryRequest;

public sealed record GetPreferenceTagsQueryResponse(IReadOnlyList<Models.PreferenceTagQueryModel> Items);
