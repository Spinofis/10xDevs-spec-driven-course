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
                x.GenerationJobId,
                x.Summary,
                x.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (planHeader is null)
            return null;

        var itemRows = await _db.PlanItems
            .AsNoTracking()
            .Where(x => x.TripId == tripId)
            .OrderBy(x => x.ItemDate)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ItemDate,
                x.ItemTime,
                x.SortOrder,
                x.PlaceType,
                x.PlaceName,
                x.Description,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var dayNumbersByDate = itemRows
            .Select(x => x.ItemDate)
            .Distinct()
            .OrderBy(x => x)
            .Select((itemDate, index) => new { itemDate, DayNumber = index + 1 })
            .ToDictionary(x => x.itemDate, x => x.DayNumber);

        var items = itemRows
            .Select(x => new PlanItemQueryModel(
                x.Id,
                dayNumbersByDate[x.ItemDate],
                x.SortOrder,
                x.PlaceName,
                x.Description,
                x.PlaceName,
                x.ItemTime,
                x.PlaceType,
                x.CreatedAt,
                UpdatedAt: x.CreatedAt))
            .ToArray();

        var status = planHeader.GenerationJobId is null
            ? PlanStatus.Saved
            : PlanStatus.Generated;

        return new PlanQueryModel(
            planHeader.TripId,
            Version: 1,
            status,
            planHeader.GenerationJobId,
            GeneratedAt: planHeader.GenerationJobId is null ? null : planHeader.UpdatedAt,
            SavedAt: planHeader.UpdatedAt,
            planHeader.Summary,
            items);
    }
}
