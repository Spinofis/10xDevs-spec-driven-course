namespace VibeTravels.Domain.Entities.Plans;

public sealed class TripPlan
{
    public Guid TripId { get; private set; }
    public Guid? GenerationJobId { get; private set; }
    public string? Title { get; private set; }
    public string? Summary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TripPlan()
    {
    }

    public static TripPlan Create(
        Guid tripId,
        Guid? generationJobId,
        string? title,
        string? summary,
        DateTimeOffset createdAt)
    {
        return new TripPlan
        {
            TripId = tripId,
            GenerationJobId = generationJobId,
            Title = title,
            Summary = summary,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateGenerated(Guid generationJobId, string? title, string? summary, DateTimeOffset updatedAt)
    {
        GenerationJobId = generationJobId;
        Title = title;
        Summary = summary;
        UpdatedAt = updatedAt;
    }
}
