using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeTravels.Application.Abstractions.Integrations;

namespace VibeTravels.Infrastructure.Integrations.OpenAI;

public sealed class OpenAiClient : IOpenAiClient
{
    private const string InvalidResponseCode = "OPENAI_INVALID_RESPONSE";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<OpenAiOptions> _optionsMonitor;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(
        HttpClient httpClient,
        IOptionsMonitor<OpenAiOptions> optionsMonitor,
        ILogger<OpenAiClient> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<OpenAiClientResult> GenerateTripPlanAsync(
        TripPlanGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return OpenAiClientResult.Failure(
                errorCode: "OPENAI_API_KEY_MISSING",
                errorMessage: "OpenAI API key is not configured.",
                isTransient: false);
        }

        var prompt = BuildUserPrompt(request);
        var requestPayload = new
        {
            model = options.Model,
            temperature = options.Temperature,
            max_tokens = options.MaxOutputTokens,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Return only JSON. Schema: {\"summary\":string|null,\"days\":[{\"date\":\"YYYY-MM-DD\",\"items\":[{\"order\":int,\"time\":\"HH:mm|null\",\"placeType\":\"attraction|restaurant|hotel\",\"placeName\":string,\"description\":string|null}]}]}"
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(requestPayload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.IsSuccessStatusCode is false)
            {
                var body = await ReadResponseBodyAsync(response, cancellationToken);
                return MapHttpFailure(response.StatusCode, body);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var rawResponse = TryGetMessageContent(document.RootElement);

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return OpenAiClientResult.Failure(
                    errorCode: InvalidResponseCode,
                    errorMessage: "OpenAI response did not contain message content.",
                    isTransient: false);
            }

            var mapped = TryMapGenerationResult(rawResponse, out var result, out var errorMessage);
            if (mapped is false || result is null)
            {
                return OpenAiClientResult.Failure(
                    errorCode: InvalidResponseCode,
                    errorMessage: errorMessage,
                    isTransient: false);
            }

            return OpenAiClientResult.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return OpenAiClientResult.Failure(
                errorCode: "OPENAI_TIMEOUT",
                errorMessage: "OpenAI request timed out.",
                isTransient: true);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "OpenAI request failed due to HTTP transport error for trip {TripId}.",
                request.TripId);

            return OpenAiClientResult.Failure(
                errorCode: "OPENAI_HTTP_ERROR",
                errorMessage: exception.Message,
                isTransient: true);
        }
    }

    private static string BuildUserPrompt(TripPlanGenerationRequest request)
    {
        var tagsText = request.Tags.Count == 0
            ? "none"
            : string.Join(", ", request.Tags.OrderBy(x => x.Order).Select(x => x.DisplayName));

        return
            $"Generate a practical trip plan.\n" +
            $"Trip title: {request.Title}\n" +
            $"Place: {request.PlaceText ?? "n/a"}\n" +
            $"Notes: {request.NoteText ?? "n/a"}\n" +
            $"Date range: {request.DateFrom:yyyy-MM-dd} to {request.DateTo:yyyy-MM-dd}\n" +
            $"Stay length range: {request.StayLengthMinDays}-{request.StayLengthMaxDays} days\n" +
            $"People count: {request.PeopleCount}\n" +
            $"Budget: {request.BudgetLevel ?? "n/a"}\n" +
            $"Pace: {request.Pace ?? "n/a"}\n" +
            $"Tags: {tagsText}\n" +
            "Each day should include 2-5 activities, 1 restaurant, hotel for staying that day and valid order values.";
    }

    private static string? TryGetMessageContent(JsonElement root)
    {
        if (root.TryGetProperty("choices", out var choices) is false
            || choices.ValueKind is not JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var first = choices[0];
        if (first.TryGetProperty("message", out var message) is false)
            return null;

        if (message.TryGetProperty("content", out var content) is false)
            return null;

        return content.GetString();
    }

    private static bool TryMapGenerationResult(
        string rawResponse,
        out TripPlanGenerationResult? result,
        out string errorMessage)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<OpenAiPlanPayload>(rawResponse, JsonOptions);
            if (payload is null)
            {
                result = null;
                errorMessage = "OpenAI response payload was null.";
                return false;
            }

            var days = payload.Days?
                .Where(x => x.Date is not null)
                .Select(x => new TripPlanGenerationDay(
                    DateOnly.Parse(x.Date!),
                    (x.Items ?? Array.Empty<OpenAiPlanItemPayload>())
                        .Select(item => new TripPlanGenerationItem(
                            item.Order,
                            TryParseTime(item.Time),
                            string.IsNullOrWhiteSpace(item.PlaceType) ? "attraction" : item.PlaceType!,
                            item.PlaceName ?? string.Empty,
                            item.Description))
                        .ToArray()))
                .ToArray() ?? Array.Empty<TripPlanGenerationDay>();

            result = new TripPlanGenerationResult(payload.Summary, days, rawResponse);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            result = null;
            errorMessage = $"Failed to parse OpenAI payload: {exception.Message}";
            return false;
        }
    }

    private static OpenAiClientResult MapHttpFailure(HttpStatusCode statusCode, string? body)
    {
        var truncated = string.IsNullOrWhiteSpace(body) ? null : body[..Math.Min(body.Length, 300)];
        var message = $"OpenAI HTTP {(int)statusCode}. {truncated}".Trim();

        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => OpenAiClientResult.Failure(
                "OPENAI_RATE_LIMITED",
                message,
                isTransient: true),
            >= HttpStatusCode.InternalServerError => OpenAiClientResult.Failure(
                "OPENAI_HTTP_ERROR",
                message,
                isTransient: true),
            _ => OpenAiClientResult.Failure(
                "OPENAI_HTTP_ERROR",
                message,
                isTransient: false)
        };
    }

    private static async Task<string?> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static TimeOnly? TryParseTime(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        return TimeOnly.TryParse(rawValue, out var parsed) ? parsed : null;
    }

    private sealed record OpenAiPlanPayload(
        string? Summary,
        IReadOnlyList<OpenAiPlanDayPayload>? Days);

    private sealed record OpenAiPlanDayPayload(
        string? Date,
        IReadOnlyList<OpenAiPlanItemPayload>? Items);

    private sealed record OpenAiPlanItemPayload(
        int Order,
        string? Time,
        string? PlaceType,
        string? PlaceName,
        string? Description);
}
