using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Domain.Entities.Trips;

public sealed class Trip
{
    public const int NoteTextMaxLength = 2000;
    private string _title = null!;
    private string? _placeText;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TripTitle Title
    {
        get => TripTitle.From(_title);
        private set => _title = value.Value;
    }

    public TripPlaceText? PlaceText
    {
        get => _placeText is null ? null : TripPlaceText.From(_placeText);
        private set => _placeText = value?.Value;
    }
    public string? NoteText { get; private set; }
    public DateOnly? DateFrom { get; private set; }
    public DateOnly? DateTo { get; private set; }
    public int? StayLengthMinDays { get; private set; }
    public int? StayLengthMaxDays { get; private set; }
    public int? PeopleCount { get; private set; }
    public string? BudgetLevel { get; private set; }
    public string? Pace { get; private set; }
    public DateTimeOffset? GeneratedAt { get; private set; }
    public bool HasGeneratedPlan { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<TripTag> TripTags { get; private set; } = new List<TripTag>();

    private Trip()
    {
    }

    public static Result<Trip> Create(
        Guid userId,
        string? title,
        string? placeText,
        string? noteText,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? stayLengthMinDays,
        int? stayLengthMaxDays,
        int? peopleCount,
        string? budgetLevel,
        string? pace,
        bool hasAnyTags)
    {
        if (userId == Guid.Empty)
            return Result<Trip>.Fail(ResultErrors.Validation("UserId is required.", nameof(UserId)));

        var stateResult = NormalizeAndValidateState(
            title,
            placeText,
            noteText,
            dateFrom,
            dateTo,
            stayLengthMinDays,
            stayLengthMaxDays,
            peopleCount,
            budgetLevel,
            pace,
            hasAnyTags,
            requireCompleteTripData: true);

        if (stateResult.IsSuccess is false || stateResult.Value is null)
            return Result<Trip>.Fail(stateResult.Errors);

        var state = stateResult.Value;

        var now = DateTimeOffset.UtcNow;
        return Result<Trip>.Ok(new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = state.Title,
            PlaceText = state.PlaceText,
            NoteText = state.NoteText,
            DateFrom = state.DateFrom,
            DateTo = state.DateTo,
            StayLengthMinDays = state.StayLengthMinDays,
            StayLengthMaxDays = state.StayLengthMaxDays,
            PeopleCount = state.PeopleCount,
            BudgetLevel = state.BudgetLevel,
            Pace = state.Pace,
            GeneratedAt = null,
            HasGeneratedPlan = false,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result ApplyPatch(
        string? title,
        string? placeText,
        string? noteText,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? stayLengthMinDays,
        int? stayLengthMaxDays,
        int? peopleCount,
        string? budgetLevel,
        string? pace,
        bool hasAnyTags)
    {
        var stateResult = NormalizeAndValidateState(
            title,
            placeText,
            noteText,
            dateFrom,
            dateTo,
            stayLengthMinDays,
            stayLengthMaxDays,
            peopleCount,
            budgetLevel,
            pace,
            hasAnyTags,
            requireCompleteTripData: false);

        if (stateResult.IsSuccess is false || stateResult.Value is null)
            return Result.Fail(stateResult.Errors);

        var changed = ApplyState(stateResult.Value);

        if (changed)
            UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Ok();
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private bool ApplyState(NormalizedTripState state)
    {
        var changed = false;

        if (_title != state.Title.Value)
        {
            _title = state.Title.Value;
            changed = true;
        }

        if (_placeText != state.PlaceText?.Value)
        {
            _placeText = state.PlaceText?.Value;
            changed = true;
        }

        if (NoteText != state.NoteText)
        {
            NoteText = state.NoteText;
            changed = true;
        }

        if (DateFrom != state.DateFrom)
        {
            DateFrom = state.DateFrom;
            changed = true;
        }

        if (DateTo != state.DateTo)
        {
            DateTo = state.DateTo;
            changed = true;
        }

        if (StayLengthMinDays != state.StayLengthMinDays)
        {
            StayLengthMinDays = state.StayLengthMinDays;
            changed = true;
        }

        if (StayLengthMaxDays != state.StayLengthMaxDays)
        {
            StayLengthMaxDays = state.StayLengthMaxDays;
            changed = true;
        }

        if (PeopleCount != state.PeopleCount)
        {
            PeopleCount = state.PeopleCount;
            changed = true;
        }

        if (BudgetLevel != state.BudgetLevel)
        {
            BudgetLevel = state.BudgetLevel;
            changed = true;
        }

        if (Pace != state.Pace)
        {
            Pace = state.Pace;
            changed = true;
        }

        return changed;
    }

    private static Result<NormalizedTripState> NormalizeAndValidateState(
        string? title,
        string? placeText,
        string? noteText,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? stayLengthMinDays,
        int? stayLengthMaxDays,
        int? peopleCount,
        string? budgetLevel,
        string? pace,
        bool hasAnyTags,
        bool requireCompleteTripData)
    {
        var titleResult = TripTitle.Create(title);
        if (titleResult.IsSuccess is false || titleResult.Value is null)
            return Result<NormalizedTripState>.Fail(titleResult.Errors);

        TripPlaceText? placeTextVo = null;
        if (string.IsNullOrWhiteSpace(placeText) is false)
        {
            var placeTextResult = TripPlaceText.Create(placeText);
            if (placeTextResult.IsSuccess is false || placeTextResult.Value is null)
                return Result<NormalizedTripState>.Fail(placeTextResult.Errors);

            placeTextVo = placeTextResult.Value;
        }

        var normalizedNoteText = string.IsNullOrWhiteSpace(noteText) ? null : noteText.Trim();

        if (placeTextVo is null && normalizedNoteText is null && hasAnyTags is false)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("At least one of PlaceText, NoteText, or Tags is required.", nameof(PlaceText)));

        if (normalizedNoteText?.Length > NoteTextMaxLength)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation($"NoteText must be at most {NoteTextMaxLength} characters.", nameof(NoteText)));

        if (requireCompleteTripData && (dateFrom is null || dateTo is null))
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("DateFrom and DateTo are required.", nameof(DateFrom)));

        if (dateFrom is not null && dateTo is not null && dateTo < dateFrom)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("DateTo must be greater than or equal to DateFrom.", nameof(DateTo)));

        if (requireCompleteTripData && (stayLengthMinDays is null || stayLengthMaxDays is null))
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("StayLengthMinDays and StayLengthMaxDays are required.", nameof(StayLengthMinDays)));

        if (stayLengthMinDays is not null && stayLengthMinDays <= 0)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("StayLengthMinDays must be greater than 0.", nameof(StayLengthMinDays)));

        if (stayLengthMaxDays is not null && stayLengthMaxDays <= 0)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("StayLengthMaxDays must be greater than 0.", nameof(StayLengthMaxDays)));

        if (stayLengthMinDays is not null && stayLengthMaxDays is not null && stayLengthMaxDays < stayLengthMinDays)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("StayLengthMaxDays must be greater than or equal to StayLengthMinDays.", nameof(StayLengthMaxDays)));

        if (requireCompleteTripData && peopleCount is null)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("PeopleCount must be greater than 0.", nameof(PeopleCount)));

        if (peopleCount is not null && peopleCount <= 0)
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("PeopleCount must be greater than 0.", nameof(PeopleCount)));

        if (budgetLevel is not null && budgetLevel is not ("Low" or "Medium" or "High"))
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("Invalid budget level.", nameof(BudgetLevel)));

        if (pace is not null && pace is not ("Relaxed" or "Normal" or "Fast"))
            return Result<NormalizedTripState>.Fail(ResultErrors.Validation("Invalid pace.", nameof(Pace)));

        return Result<NormalizedTripState>.Ok(new NormalizedTripState(
            titleResult.Value,
            placeTextVo,
            normalizedNoteText,
            dateFrom,
            dateTo,
            stayLengthMinDays,
            stayLengthMaxDays,
            peopleCount,
            budgetLevel,
            pace));
    }

    private sealed record NormalizedTripState(
        TripTitle Title,
        TripPlaceText? PlaceText,
        string? NoteText,
        DateOnly? DateFrom,
        DateOnly? DateTo,
        int? StayLengthMinDays,
        int? StayLengthMaxDays,
        int? PeopleCount,
        string? BudgetLevel,
        string? Pace);
}
