using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Plans.Queries.Models;

namespace VibeTravels.Application.Features.Plans.Services;

public sealed class TripPlanReadService : ITripPlanReadService
{
    private readonly IAppDbContext _db;

    public TripPlanReadService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PlanQueryModel?> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var planHeader = await _db.TripPlans
            .AsNoTracking()
            .Where(x => x.TripId == tripId)
            .Select(x => new
            {
                x.TripId,
                x.Version,
                x.Status,
                x.GenerationJobId,
                x.GeneratedAt,
                x.SavedAt,
                x.Summary,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (planHeader is null)
            return null;

        var itemRows = await _db.PlanItems
            .AsNoTracking()
            .Where(x => x.TripId == tripId)
            .OrderBy(x => x.DayNumber)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.DayNumber,
                x.ItemDate,
                x.SortOrder,
                x.PlaceType,
                x.Title,
                x.Description,
                x.LocationText,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var items = itemRows
            .Select(x => new PlanItemQueryModel(
                x.Id,
                x.DayNumber,
                x.SortOrder,
                x.Title,
                x.ItemDate,
                x.Description,
                x.LocationText,
                x.PlaceType,
                x.CreatedAt,
                x.UpdatedAt))
            .ToArray();

        return new PlanQueryModel(
            planHeader.TripId,
            planHeader.Version,
            (PlanStatus)planHeader.Status,
            planHeader.GenerationJobId,
            planHeader.GeneratedAt,
            planHeader.SavedAt,
            planHeader.Summary,
            items);
    }
}
