namespace VibeTravels.Application.Features.Legacy.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresIn);

public sealed record LogoutResponse;
