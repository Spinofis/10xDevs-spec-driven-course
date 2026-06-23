using VibeTravels.Application.Features.Common;
using VibeTravels.Domain.Entities.Jobs;

namespace VibeTravels.Application.Features.Jobs.Services;

public sealed class GenerationJobStatusMapper : IGenerationJobStatusMapper
{
    public GenerationJobStatus Map(AiGenerationJobStatus status)
    {
        return status switch
        {
            AiGenerationJobStatus.Pending => GenerationJobStatus.Queued,
            AiGenerationJobStatus.Running => GenerationJobStatus.Processing,
            AiGenerationJobStatus.Succeeded => GenerationJobStatus.Succeeded,
            AiGenerationJobStatus.Failed => GenerationJobStatus.Failed,
            AiGenerationJobStatus.Canceled => GenerationJobStatus.Canceled,
            _ => GenerationJobStatus.Failed
        };
    }
}
