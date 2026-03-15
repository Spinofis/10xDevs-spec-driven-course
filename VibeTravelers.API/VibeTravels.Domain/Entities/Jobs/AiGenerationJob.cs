using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.Entities.Jobs;

public sealed class AiGenerationJob
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public AiGenerationJobStatus Status { get; private set; } = AiGenerationJobStatus.Pending;
    public string InputSnapshot { get; private set; } = "{}";
    public string InputHash { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AiGenerationJob()
    {
    }

    public static Result<AiGenerationJob> CreatePending(
        Guid tripId,
        string inputSnapshot,
        string inputHash,
        DateTimeOffset requestedAt)
    {
        if (tripId == Guid.Empty)
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("TripId is required.", nameof(TripId)));

        if (string.IsNullOrWhiteSpace(inputSnapshot))
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("InputSnapshot is required.", nameof(InputSnapshot)));

        if (string.IsNullOrWhiteSpace(inputHash))
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("InputHash is required.", nameof(InputHash)));

        return Result<AiGenerationJob>.Ok(new AiGenerationJob
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            Status = AiGenerationJobStatus.Pending,
            InputSnapshot = inputSnapshot,
            InputHash = inputHash,
            RequestedAt = requestedAt,
            StartedAt = null,
            FinishedAt = null,
            CanceledAt = null,
            ErrorMessage = null,
        });
    }
}
