using System;
using VibeTravels.Application.Features.Legacy.Jobs.Models;

namespace VibeTravels.Application.Features.Legacy.Jobs.Commands;

public sealed record QueueGenerationJobCommandRequest(Guid TripId, bool? UseProfileDefaults);

public sealed record QueueGenerationJobCommandResponse(GenerationJobDto Job);
