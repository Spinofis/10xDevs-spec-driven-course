namespace VibeTravels.Infrastructure.Integrations.OpenAI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string Model { get; set; } = "gpt-4.1-mini";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxOutputTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.2;
}
