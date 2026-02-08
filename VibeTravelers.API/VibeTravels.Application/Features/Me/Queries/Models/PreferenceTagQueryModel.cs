using VibeTravels.Application.Features.Tags.Queries.Models;

namespace VibeTravels.Application.Features.Me.Queries.Models;

public sealed record PreferenceTagQueryModel(
    TagQueryModel Tag,
    int Order,
    DateTimeOffset CreatedAt);
