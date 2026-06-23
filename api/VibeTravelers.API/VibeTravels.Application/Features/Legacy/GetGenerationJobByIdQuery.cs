using System;
using VibeTravels.Application.Features.Legacy.Jobs.Models;

namespace VibeTravels.Application.Features.Legacy.Jobs.Queries;

public sealed record GetGenerationJobByIdQueryRequest(Guid JobId);

public sealed record GetGenerationJobByIdQueryResponse(GenerationJobDto Job);
