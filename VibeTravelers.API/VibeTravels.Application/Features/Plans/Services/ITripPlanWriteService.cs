using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Services;

public interface ITripPlanWriteService
{
    Task ReplacePlanItemsAsync(
        TripPlan plan,
        IReadOnlyList<PlanItemCommandModel> items,
        CancellationToken cancellationToken);
}
