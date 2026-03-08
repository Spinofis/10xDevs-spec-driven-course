using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record PatchTripCommand : IRequest<Result<PatchTripCommandResponse>>
{
    public required Guid UserId { get; init; }
    public required Guid TripId { get; init; }
    public required PatchTripCommandRequest Request { get; init; }
}
