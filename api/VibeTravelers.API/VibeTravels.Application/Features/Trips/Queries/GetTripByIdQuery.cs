using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record GetTripByIdQuery(Guid UserId, GetTripByIdQueryRequest Request)
    : IRequest<Result<GetTripByIdQueryResponse>>;
