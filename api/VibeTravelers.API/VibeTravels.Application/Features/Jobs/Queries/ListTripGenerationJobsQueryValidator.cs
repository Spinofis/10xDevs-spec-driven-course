using FluentValidation;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed class ListTripGenerationJobsQueryValidator : AbstractValidator<ListTripGenerationJobsQuery>
{
    public ListTripGenerationJobsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new ListTripGenerationJobsQueryRequestValidator()!);
    }
}

public sealed class ListTripGenerationJobsQueryRequestValidator : AbstractValidator<ListTripGenerationJobsQueryRequest>
{
    public ListTripGenerationJobsQueryRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .When(x => x.Limit is not null)
            .WithMessage("Limit must be between 1 and 100.");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor)
            .When(x => string.IsNullOrWhiteSpace(x.Cursor) is false)
            .WithMessage("Invalid cursor.");
    }

    private static bool BeValidCursor(string? cursor)
        => ListTripGenerationJobsCursor.TryDecode(cursor, out _);
}
