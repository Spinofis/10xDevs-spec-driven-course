using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed record RegisterUserCommand(RegisterUserCommandRequest Request) : IRequest<Result>;
