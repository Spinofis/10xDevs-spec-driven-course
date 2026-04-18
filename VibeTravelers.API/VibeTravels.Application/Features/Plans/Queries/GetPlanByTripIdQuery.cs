using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Plans.Queries;

public sealed record GetPlanByTripIdQuery(Guid UserId, GetPlanByTripIdQueryRequest Request)
    : IRequest<Result<GetPlanByTripIdQueryResponse>>;
