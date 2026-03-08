using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed record ListTripsQuery(Guid UserId, ListTripsQueryRequest Request)
    : IRequest<Result<ListTripsQueryResponse>>;
