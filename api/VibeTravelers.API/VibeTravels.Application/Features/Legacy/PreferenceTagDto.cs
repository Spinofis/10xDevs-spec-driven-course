using VibeTravels.Application.Features.Legacy.Tags.Models;

namespace VibeTravels.Application.Features.Legacy.Me.Models;

public sealed record PreferenceTagDto(
    TagDto Tag,
    int Order);
