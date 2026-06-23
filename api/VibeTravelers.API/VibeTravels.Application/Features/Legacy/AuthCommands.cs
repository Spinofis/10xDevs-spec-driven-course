using VibeTravels.Application.Features.Legacy.Auth;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed record RegisterUserCommand(RegisterRequest Request);

public sealed record LoginCommand(LoginRequest Request);

public sealed record LogoutCommand;
