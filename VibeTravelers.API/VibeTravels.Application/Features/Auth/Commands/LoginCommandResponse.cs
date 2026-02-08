namespace VibeTravels.Application.Features.Auth.Commands;

public sealed record LoginCommandResponse(string AccessToken, int ExpiresIn);
