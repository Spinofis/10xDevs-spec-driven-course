using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Domain.Entities.Users;

public sealed class UserPreferenceTag
{
    public Guid UserId { get; private set; }
    public Guid TagId { get; private set; }
    public int Order { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Tag Tag { get; private set; } = null!;

    private UserPreferenceTag() { }

    public static UserPreferenceTag Create(Guid userId, Guid tagId, int order)
    {
        return new UserPreferenceTag
        {
            UserId = userId,
            TagId = tagId,
            Order = order,
            CreatedAt = DateTime.UtcNow
        };
    }
}
