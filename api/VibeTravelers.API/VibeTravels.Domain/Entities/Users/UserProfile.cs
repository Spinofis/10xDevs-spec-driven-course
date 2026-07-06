namespace VibeTravels.Domain.Entities.Users;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string? DefaultBudgetLevel { get; private set; }
    public int? DefaultPeopleCount { get; private set; }
    public string? DefaultPace { get; private set; }
    public string? DefaultNotes { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new UserProfile
        {
            UserId = userId,
            DefaultBudgetLevel = null,
            DefaultPeopleCount = null,
            DefaultPace = null,
            DefaultNotes = null,
            IsDefault = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string? defaultBudgetLevel,
        int? defaultPeopleCount,
        string? defaultPace,
        string? defaultNotes,
        bool isDefault)
    {
        DefaultBudgetLevel = defaultBudgetLevel;
        DefaultPeopleCount = defaultPeopleCount;
        DefaultPace = defaultPace;
        DefaultNotes = defaultNotes;
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }
}
