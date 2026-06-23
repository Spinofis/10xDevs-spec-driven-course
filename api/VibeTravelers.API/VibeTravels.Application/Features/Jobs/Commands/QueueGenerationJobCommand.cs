using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Jobs.Commands;

public sealed record QueueGenerationJobCommand(Guid UserId, QueueGenerationJobCommandRequest Request)
    : IRequest<Result<QueueGenerationJobCommandResponse>>;
