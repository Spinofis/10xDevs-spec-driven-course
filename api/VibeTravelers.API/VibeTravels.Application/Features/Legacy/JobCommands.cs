using System;
using VibeTravels.Application.Features.Legacy.Jobs;

namespace VibeTravels.Application.Features.Jobs.Commands;

public sealed record QueueGenerationJobCommand(Guid TripId, QueueGenerationJobRequest Request);
