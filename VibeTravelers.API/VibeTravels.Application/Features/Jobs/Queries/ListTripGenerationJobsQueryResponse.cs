using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Jobs.Queries.Models;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed record ListTripGenerationJobsQueryResponse(
    IReadOnlyList<GenerationJobListItemQueryModel> Items,
    string? NextCursor)
    : PagedResponse<GenerationJobListItemQueryModel>(Items, NextCursor);
