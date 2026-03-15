using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.Entities.Jobs;

public sealed class AiGenerationJob
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid UserId { get; private set; }
    public AiGenerationJobStatus Status { get; private set; } = AiGenerationJobStatus.Pending;
    public string InputSnapshot { get; private set; } = "{}";
    public string InputHash { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }
    public int AttemptNo { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool Discarded { get; private set; }
    public string? DiscardReason { get; private set; }

    private AiGenerationJob()
    {
    }

    public static Result<AiGenerationJob> CreatePending(
        Guid tripId,
        Guid userId,
        string inputSnapshot,
        string inputHash,
        DateTimeOffset requestedAt)
    {
        if (tripId == Guid.Empty)
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("TripId is required.", nameof(TripId)));

        if (userId == Guid.Empty)
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("UserId is required.", nameof(UserId)));

        if (string.IsNullOrWhiteSpace(inputSnapshot))
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("InputSnapshot is required.", nameof(InputSnapshot)));

        if (string.IsNullOrWhiteSpace(inputHash))
            return Result<AiGenerationJob>.Fail(ResultErrors.Validation("InputHash is required.", nameof(InputHash)));

        return Result<AiGenerationJob>.Ok(new AiGenerationJob
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            UserId = userId,
            Status = AiGenerationJobStatus.Pending,
            InputSnapshot = inputSnapshot,
            InputHash = inputHash,
            RequestedAt = requestedAt,
            StartedAt = null,
            FinishedAt = null,
            CanceledAt = null,
            AttemptNo = 0,
            ErrorCode = null,
            ErrorMessage = null,
            Discarded = false,
            DiscardReason = null,
        });
    }
}
