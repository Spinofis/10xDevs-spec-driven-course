using System.Text;
using System.Text.Json;

namespace VibeTravels.Application.Features.Jobs.Queries;

internal static class ListTripGenerationJobsCursor
{
    internal sealed record Payload(
        int V,
        string Field,
        bool Desc,
        string LastRequestedAt,
        Guid LastId);

    internal static bool TryDecode(string? cursor, out Payload payload)
    {
        payload = default!;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(cursor.Trim()));
            var parsed = JsonSerializer.Deserialize<Payload>(json);
            if (parsed is null)
                return false;

            if (parsed.V != 1)
                return false;

            if (parsed.Field != "requestedAt" || parsed.Desc is false)
                return false;

            if (parsed.LastId == Guid.Empty)
                return false;

            if (DateTimeOffset.TryParse(parsed.LastRequestedAt, out _) is false)
                return false;

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string Encode(DateTimeOffset lastRequestedAt, Guid lastId)
    {
        var payload = new Payload(
            V: 1,
            Field: "requestedAt",
            Desc: true,
            LastRequestedAt: lastRequestedAt.ToString("O"),
            LastId: lastId);

        var json = JsonSerializer.Serialize(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 0:
                break;
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
            default:
                throw new FormatException("Invalid base64url length.");
        }

        return Convert.FromBase64String(padded);
    }
}
