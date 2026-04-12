namespace VibeTravels.Domain.Entities.Plans;

public sealed class PlanItem
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public DateOnly ItemDate { get; private set; }
    public TimeOnly? ItemTime { get; private set; }
    public int SortOrder { get; private set; }
    public PlanItemPlaceType PlaceType { get; private set; }
    public string PlaceName { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PlanItem()
    {
    }

    public static PlanItem Create(
        Guid tripId,
        DateOnly itemDate,
        TimeOnly? itemTime,
        int sortOrder,
        PlanItemPlaceType placeType,
        string placeName,
        string? description,
        DateTimeOffset createdAt)
    {
        return new PlanItem
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            ItemDate = itemDate,
            ItemTime = itemTime,
            SortOrder = sortOrder,
            PlaceType = placeType,
            PlaceName = placeName,
            Description = description,
            CreatedAt = createdAt
        };
    }
}
