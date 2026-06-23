using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Services;

public interface ITripPlanReadService
{
    Task<PlanQueryModel?> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);
}
