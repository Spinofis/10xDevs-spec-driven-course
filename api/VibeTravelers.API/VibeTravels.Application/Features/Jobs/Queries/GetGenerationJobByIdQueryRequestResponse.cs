using VibeTravels.Application.Features.Jobs.Queries.Models;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed record GetGenerationJobByIdQueryRequest(Guid JobId);

public sealed record GetGenerationJobByIdQueryResponse(GenerationJobQueryModel Job);
