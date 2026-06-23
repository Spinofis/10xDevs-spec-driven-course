namespace VibeTravels.Application.Features.Common;

public enum BudgetLevel
{
    Low,
    Medium,
    High
}

public enum Pace
{
    Relaxed,
    Normal,
    Fast
}

public enum CostLevel
{
    Low,
    Medium,
    High
}

public enum PlanStatus
{
    Generated,
    Saved
}

public enum GenerationJobStatus
{
    Queued,
    Processing,
    Succeeded,
    Failed,
    Canceled
}

public enum InputSnapshotKind
{
    BeforeGeneration,
    AfterGeneration
}
