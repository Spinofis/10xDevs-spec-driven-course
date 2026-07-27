namespace VibeTravels.Domain.Entities.Plans;

public sealed class TripPlan
{
    public Guid TripId { get; private set; }
    public Guid? GenerationJobId { get; private set; }
    public string? Title { get; private set; }
    public string? Summary { get; private set; }
    public int Version { get; private set; }
    public TripPlanStatus Status { get; private set; }
    public DateTimeOffset? GeneratedAt { get; private set; }
    public DateTimeOffset? SavedAt { get; private set; }
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
            Version = 1,
            Status = generationJobId is null ? TripPlanStatus.Saved : TripPlanStatus.Generated,
            GeneratedAt = generationJobId is null ? null : createdAt,
            SavedAt = generationJobId is null ? createdAt : null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateGenerated(Guid generationJobId, string? title, string? summary, DateTimeOffset updatedAt)
    {
        GenerationJobId = generationJobId;
        Title = title;
        Summary = summary;
        Version++;
        Status = TripPlanStatus.Generated;
        GeneratedAt = updatedAt;
        UpdatedAt = updatedAt;
    }

    public void UpdateManual(string? summary, DateTimeOffset updatedAt)
    {
        Summary = summary;
        Version++;
        Status = TripPlanStatus.Saved;
        SavedAt = updatedAt;
        UpdatedAt = updatedAt;
    }

}
