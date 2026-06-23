using VibeTravels.Application.Features.Common;
using VibeTravels.Domain.Entities.Jobs;

namespace VibeTravels.Application.Features.Jobs.Services;

public interface IGenerationJobStatusMapper
{
    GenerationJobStatus Map(AiGenerationJobStatus status);
}
