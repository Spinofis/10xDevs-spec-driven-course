using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Services;

public sealed class TripPlanWriteService : ITripPlanWriteService
{
    private readonly IAppDbContext _db;

    public TripPlanWriteService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task ReplacePlanItemsAsync(
        TripPlan plan,
        IReadOnlyList<PlanItemCommandModel> items,
        CancellationToken cancellationToken)
    {
        var existingItems = await _db.PlanItems
            .Where(x => x.TripId == plan.TripId)
            .ToListAsync(cancellationToken);

        var existingById = existingItems.ToDictionary(x => x.Id);
        var requestedIds = items.Select(x => x.Id).ToHashSet();

        var toRemove = existingItems
            .Where(x => requestedIds.Contains(x.Id) is false)
            .ToArray();
        if (toRemove.Length > 0)
            _db.PlanItems.RemoveRange(toRemove);

        foreach (var requestedItem in items.OrderBy(x => x.DayNumber).ThenBy(x => x.Order).ThenBy(x => x.Id))
        {
            var normalizedTitle = requestedItem.Title.Trim();
            var normalizedDescription = string.IsNullOrWhiteSpace(requestedItem.Description) ? null : requestedItem.Description.Trim();
            var normalizedLocation = string.IsNullOrWhiteSpace(requestedItem.LocationText) ? null : requestedItem.LocationText.Trim();

            if (existingById.TryGetValue(requestedItem.Id, out var existingItem))
            {
                existingItem.UpdateManual(
                    dayNumber: requestedItem.DayNumber,
                    itemDate: requestedItem.ItemDate,
                    sortOrder: requestedItem.Order,
                    title: normalizedTitle,
                    description: normalizedDescription,
                    locationText: normalizedLocation,
                    placeType: requestedItem.PlaceType,
                    createdAt: requestedItem.CreatedAt,
                    updatedAt: requestedItem.UpdatedAt);
                continue;
            }

            _db.PlanItems.Add(PlanItem.CreateManual(
                id: requestedItem.Id,
                tripId: plan.TripId,
                dayNumber: requestedItem.DayNumber,
                itemDate: requestedItem.ItemDate,
                sortOrder: requestedItem.Order,
                title: normalizedTitle,
                description: normalizedDescription,
                locationText: normalizedLocation,
                placeType: requestedItem.PlaceType,
                createdAt: requestedItem.CreatedAt,
                updatedAt: requestedItem.UpdatedAt));
        }
    }
}
