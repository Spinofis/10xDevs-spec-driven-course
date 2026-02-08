using MediatR;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed record RegisterUserCommand(RegisterUserCommandRequest Request) : IRequest<RegisterUserCommandResponse>;
