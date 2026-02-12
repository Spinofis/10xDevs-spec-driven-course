namespace VibeTravels.Domain.Entities.Tags;

public sealed class Tag
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Tag()
    {
    }
}
