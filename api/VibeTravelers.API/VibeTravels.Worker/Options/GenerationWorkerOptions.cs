namespace VibeTravels.Worker.Options;

public sealed class GenerationWorkerOptions
{
    public const string SectionName = "GenerationWorker";

    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 5;
    public int EmptyPollDelaySeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 10;
    public int MaxParallelJobs { get; set; } = 4;
    public int MaxAttempts { get; set; } = 3;
    public int StaleProcessingAfterMinutes { get; set; } = 10;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
}
