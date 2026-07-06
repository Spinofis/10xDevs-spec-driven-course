using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Me.Queries;

public sealed record GetUserProfileQuery(Guid UserId, GetUserProfileQueryRequest Request)
    : IRequest<Result<GetUserProfileQueryResponse>>;
