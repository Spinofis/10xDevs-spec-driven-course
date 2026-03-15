using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed record GetGenerationJobByIdQuery(Guid UserId, GetGenerationJobByIdQueryRequest Request)
    : IRequest<Result<GetGenerationJobByIdQueryResponse>>;
