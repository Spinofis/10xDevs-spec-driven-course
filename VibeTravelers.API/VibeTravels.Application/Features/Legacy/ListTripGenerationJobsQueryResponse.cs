using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Jobs.Models;

namespace VibeTravels.Application.Features.Legacy.Jobs.Queries;

public sealed record ListTripGenerationJobsQueryResponse(
    IReadOnlyList<GenerationJobListItemDto> Items)
    : PagedResponse<GenerationJobListItemDto>(Items);
