using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed record UpdatePlanCommandRequest
{
    public Guid TripId { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<PlanItemCommandModel> Items { get; init; } = Array.Empty<PlanItemCommandModel>();

    public UpdatePlanCommandRequest()
    {
    }

    public UpdatePlanCommandRequest(Guid tripId, string? summary, IReadOnlyList<PlanItemCommandModel> items)
    {
        TripId = tripId;
        Summary = summary;
        Items = items;
    }

    public UpdatePlanCommandModel ToModel()
        => new(Summary, Items);
}

public sealed record UpdatePlanCommandResponse(PlanQueryModel Plan);
