using System.Text.Json;

namespace VibeTravels.Application.Features.Jobs.Models;

public sealed record GenerationJobRequestPayload(
    Guid TripId,
    Guid UserId,
    string Title,
    string? PlaceText,
    string? NoteText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? StayLengthMinDays,
    int? StayLengthMaxDays,
    int? PeopleCount,
    string? BudgetLevel,
    string? Pace,
    IReadOnlyList<GenerationJobRequestPayloadTag> Tags)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static bool TryFromJson(string json, out GenerationJobRequestPayload? payload)
    {
        try
        {
            payload = JsonSerializer.Deserialize<GenerationJobRequestPayload>(json, JsonOptions);
            return payload is not null;
        }
        catch
        {
            payload = null;
            return false;
        }
    }
}

public sealed record GenerationJobRequestPayloadTag(
    Guid TagId,
    string Code,
    string DisplayName,
    int Order);
