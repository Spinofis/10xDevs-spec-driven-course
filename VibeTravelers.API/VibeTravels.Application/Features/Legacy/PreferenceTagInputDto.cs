namespace VibeTravels.Application.Features.Legacy.Me.Models;

public sealed record PreferenceTagInputDto(
    Guid TagId,
    int Order);
