namespace VibeTravels.Application.Features.Legacy.Auth.Commands;

public sealed record LoginCommandResponse(string AccessToken, int ExpiresIn);
