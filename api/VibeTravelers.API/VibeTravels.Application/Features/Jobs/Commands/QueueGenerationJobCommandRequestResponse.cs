using VibeTravels.Application.Features.Jobs.Queries.Models;

namespace VibeTravels.Application.Features.Jobs.Commands;

public sealed record QueueGenerationJobCommandRequest(Guid TripId);

public sealed record QueueGenerationJobCommandResponse(QueuedGenerationJobQueryModel Job);
