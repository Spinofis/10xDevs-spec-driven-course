using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record DeleteTripCommand(Guid UserId, DeleteTripCommandRequest Request)
    : IRequest<Result>;
