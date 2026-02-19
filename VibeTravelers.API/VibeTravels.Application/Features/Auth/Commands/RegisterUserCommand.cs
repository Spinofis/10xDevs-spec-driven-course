using MediatR;
using VibeTravels.Application.Common.Results;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed record RegisterUserCommand(RegisterUserCommandRequest Request) : IRequest<Result>;
