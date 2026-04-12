namespace VibeTravels.Worker.Processing;

internal static class GenerationJobErrorCodes
{
    public const string OpenAiTimeout = "OPENAI_TIMEOUT";
    public const string OpenAiRateLimited = "OPENAI_RATE_LIMITED";
    public const string OpenAiHttpError = "OPENAI_HTTP_ERROR";
    public const string OpenAiInvalidResponse = "OPENAI_INVALID_RESPONSE";
    public const string JobPayloadInvalid = "JOB_PAYLOAD_INVALID";
    public const string PlanPersistFailed = "PLAN_PERSIST_FAILED";
    public const string WorkerStaleJob = "WORKER_STALE_JOB";
    public const string NewerJobExists = "NEWER_JOB_EXISTS";
}
