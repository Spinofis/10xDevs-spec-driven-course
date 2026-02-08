using VibeTravels.Application.Features.Jobs.Commands.Models;
using VibeTravels.Application.Features.Jobs.Queries.Models;

namespace VibeTravels.Application.Features.Jobs.Commands;

public sealed record QueueGenerationJobCommandRequest(Guid TripId, QueueGenerationJobCommandModel? Model);

public sealed record QueueGenerationJobCommandResponse(GenerationJobQueryModel Job);
