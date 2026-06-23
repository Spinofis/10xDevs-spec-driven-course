namespace VibeTravels.Application.Abstractions.Integrations;

public interface IOpenAiClient
{
    Task<OpenAiClientResult> GenerateTripPlanAsync(
        TripPlanGenerationRequest request,
        CancellationToken cancellationToken);
}
