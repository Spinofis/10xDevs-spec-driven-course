using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed record ListTripGenerationJobsQuery(Guid UserId, ListTripGenerationJobsQueryRequest Request)
    : IRequest<Result<ListTripGenerationJobsQueryResponse>>;
