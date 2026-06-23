namespace VibeTravels.Application.Abstractions.Integrations;

public sealed record TripPlanGenerationRequest(
    Guid TripId,
    Guid UserId,
    string Title,
    string? PlaceText,
    string? NoteText,
    DateOnly DateFrom,
    DateOnly DateTo,
    int StayLengthMinDays,
    int StayLengthMaxDays,
    int PeopleCount,
    string? BudgetLevel,
    string? Pace,
    IReadOnlyList<TripPlanGenerationTag> Tags);

public sealed record TripPlanGenerationTag(
    Guid TagId,
    string Code,
    string DisplayName,
    int Order);

public sealed record TripPlanGenerationResult(
    string? Summary,
    IReadOnlyList<TripPlanGenerationDay> Days,
    string RawResponsePayload);

public sealed record TripPlanGenerationDay(
    DateOnly Date,
    IReadOnlyList<TripPlanGenerationItem> Items);

public sealed record TripPlanGenerationItem(
    int Order,
    TimeOnly? Time,
    string PlaceType,
    string PlaceName,
    string? Description);

public sealed record OpenAiClientResult
{
    private OpenAiClientResult(
        bool isSuccess,
        TripPlanGenerationResult? result,
        string? errorCode,
        string? errorMessage,
        bool isTransient)
    {
        IsSuccess = isSuccess;
        Result = result;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        IsTransient = isTransient;
    }

    public bool IsSuccess { get; }
    public TripPlanGenerationResult? Result { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsTransient { get; }

    public static OpenAiClientResult Success(TripPlanGenerationResult result)
        => new(true, result, null, null, false);

    public static OpenAiClientResult Failure(string errorCode, string? errorMessage, bool isTransient)
        => new(false, null, errorCode, errorMessage, isTransient);
}
