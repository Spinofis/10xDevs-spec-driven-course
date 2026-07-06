using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Me.Commands;

public sealed record UpsertUserProfileCommand(Guid UserId, UpsertUserProfileCommandRequest Request)
    : IRequest<Result>;
