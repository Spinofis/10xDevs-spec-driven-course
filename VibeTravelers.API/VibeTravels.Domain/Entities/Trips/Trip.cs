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

        var titleResult = TripTitle.Create(title);
        if (titleResult.IsSuccess is false || titleResult.Value is null)
            return Result<Trip>.Fail(titleResult.Errors);

        TripPlaceText? placeTextVo = null;
        if (string.IsNullOrWhiteSpace(placeText) is false)
        {
            var placeTextResult = TripPlaceText.Create(placeText);
            if (placeTextResult.IsSuccess is false || placeTextResult.Value is null)
                return Result<Trip>.Fail(placeTextResult.Errors);

            placeTextVo = placeTextResult.Value;
        }

        var normalizedNoteText = string.IsNullOrWhiteSpace(noteText) ? null : noteText.Trim();

        if (placeTextVo is null && normalizedNoteText is null && hasAnyTags is false)
            return Result<Trip>.Fail(ResultErrors.Validation("At least one of PlaceText, NoteText, or Tags is required.", nameof(PlaceText)));

        if (noteText?.Length > NoteTextMaxLength)
            return Result<Trip>.Fail(ResultErrors.Validation($"NoteText must be at most {NoteTextMaxLength} characters.", nameof(NoteText)));

        if (dateFrom is null || dateTo is null)
            return Result<Trip>.Fail(ResultErrors.Validation("DateFrom and DateTo are required.", nameof(DateFrom)));

        if (dateTo < dateFrom)
            return Result<Trip>.Fail(ResultErrors.Validation("DateTo must be greater than or equal to DateFrom.", nameof(DateTo)));

        if (stayLengthMinDays is null || stayLengthMaxDays is null)
            return Result<Trip>.Fail(ResultErrors.Validation("StayLengthMinDays and StayLengthMaxDays are required.", nameof(StayLengthMinDays)));

        if (stayLengthMinDays <= 0 || stayLengthMaxDays <= 0)
            return Result<Trip>.Fail(ResultErrors.Validation("Stay length values must be greater than 0.", nameof(StayLengthMinDays)));

        if (stayLengthMaxDays < stayLengthMinDays)
            return Result<Trip>.Fail(ResultErrors.Validation("StayLengthMaxDays must be greater than or equal to StayLengthMinDays.", nameof(StayLengthMaxDays)));

        if (peopleCount is null || peopleCount <= 0)
            return Result<Trip>.Fail(ResultErrors.Validation("PeopleCount must be greater than 0.", nameof(PeopleCount)));

        if (budgetLevel is not null && budgetLevel is not ("Low" or "Medium" or "High"))
            return Result<Trip>.Fail(ResultErrors.Validation("Invalid budget level.", nameof(BudgetLevel)));

        if (pace is not null && pace is not ("Relaxed" or "Normal" or "Fast"))
            return Result<Trip>.Fail(ResultErrors.Validation("Invalid pace.", nameof(Pace)));

        var now = DateTimeOffset.UtcNow;
        return Result<Trip>.Ok(new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = titleResult.Value,
            PlaceText = placeTextVo,
            NoteText = normalizedNoteText,
            DateFrom = dateFrom,
            DateTo = dateTo,
            StayLengthMinDays = stayLengthMinDays,
            StayLengthMaxDays = stayLengthMaxDays,
            PeopleCount = peopleCount,
            BudgetLevel = budgetLevel,
            Pace = pace,
            GeneratedAt = null,
            HasGeneratedPlan = false,
            CreatedAt = now,
            UpdatedAt = now
        });
    }
}
