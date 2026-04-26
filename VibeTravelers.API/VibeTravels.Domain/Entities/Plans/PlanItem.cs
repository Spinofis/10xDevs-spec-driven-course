namespace VibeTravels.Domain.Entities.Plans;

public sealed class PlanItem
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public int DayNumber { get; private set; }
    public DateTimeOffset ItemDate { get; private set; }
    public int SortOrder { get; private set; }
    public PlanItemPlaceType PlaceType { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LocationText { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PlanItem()
    {
    }

    public static PlanItem CreateGenerated(
        Guid tripId,
        int dayNumber,
        DateTimeOffset itemDate,
        int sortOrder,
        PlanItemPlaceType placeType,
        string title,
        string? description,
        string? locationText,
        DateTimeOffset createdAt)
    {
        return new PlanItem
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            DayNumber = dayNumber,
            ItemDate = itemDate,
            SortOrder = sortOrder,
            PlaceType = placeType,
            Title = title,
            Description = description,
            LocationText = locationText,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static PlanItem CreateManual(
        Guid id,
        Guid tripId,
        int dayNumber,
        DateTimeOffset itemDate,
        int sortOrder,
        string title,
        string? description,
        string? locationText,
        PlanItemPlaceType placeType,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new PlanItem
        {
            Id = id,
            TripId = tripId,
            DayNumber = dayNumber,
            ItemDate = itemDate,
            SortOrder = sortOrder,
            PlaceType = placeType,
            Title = title,
            Description = description,
            LocationText = locationText,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public bool UpdateManual(
        int dayNumber,
        DateTimeOffset itemDate,
        int sortOrder,
        string title,
        string? description,
        string? locationText,
        PlanItemPlaceType placeType,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var changed = false;

        if (DayNumber != dayNumber)
        {
            DayNumber = dayNumber;
            changed = true;
        }

        if (ItemDate != itemDate)
        {
            ItemDate = itemDate;
            changed = true;
        }

        if (SortOrder != sortOrder)
        {
            SortOrder = sortOrder;
            changed = true;
        }

        if (Title != title)
        {
            Title = title;
            changed = true;
        }

        if (Description != description)
        {
            Description = description;
            changed = true;
        }

        if (LocationText != locationText)
        {
            LocationText = locationText;
            changed = true;
        }

        if (PlaceType != placeType)
        {
            PlaceType = placeType;
            changed = true;
        }

        if (CreatedAt != createdAt)
        {
            CreatedAt = createdAt;
            changed = true;
        }

        if (UpdatedAt != updatedAt)
        {
            UpdatedAt = updatedAt;
            changed = true;
        }

        return changed;
    }
}
