using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record CreateTripCommand(Guid UserId, CreateTripCommandRequest Request)
    : IRequest<Result<CreateTripCommandResponse>>;
