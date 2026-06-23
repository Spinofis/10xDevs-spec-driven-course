using FluentValidation;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed class ListTripsQueryValidator : AbstractValidator<ListTripsQuery>
{
    public ListTripsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new ListTripsQueryRequestValidator()!);
    }
}

public sealed class ListTripsQueryRequestValidator : AbstractValidator<ListTripsQueryRequest>
{
    public ListTripsQueryRequestValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .When(x => x.Limit is not null)
            .WithMessage("Limit must be between 1 and 100.");

        RuleFor(x => x.Query)
            .MaximumLength(200)
            .When(x => string.IsNullOrWhiteSpace(x.Query) is false);

        RuleFor(x => x.Sort)
            .Must(BeValidSort)
            .When(x => string.IsNullOrWhiteSpace(x.Sort) is false)
            .WithMessage("Invalid sort value.");

        RuleFor(x => x.Cursor)
            .Must((request, cursor) => BeValidCursorForSort(cursor, request.Sort))
            .When(x => string.IsNullOrWhiteSpace(x.Cursor) is false)
            .WithMessage("Invalid cursor.");
    }

    private static bool BeValidSort(string? sort)
    {
        try
        {
            _ = ListTripsCursor.ParseSortOrDefault(sort);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidCursorForSort(string? cursor, string? sort)
    {
        if (ListTripsCursor.TryDecode(cursor, out var payload) is false)
            return false;

        ListTripsCursor.SortSpec requestedSort;
        try
        {
            requestedSort = ListTripsCursor.ParseSortOrDefault(sort);
        }
        catch
        {
            return false;
        }

        if (payload.Field != requestedSort.Field || payload.Desc != requestedSort.Desc)
            return false;

        return payload.Field switch
        {
            ListTripsCursor.SortField.CreatedAt => DateTimeOffset.TryParse(payload.LastValue, out _),
            ListTripsCursor.SortField.GeneratedAt => payload.LastIsNull is not null
                && (payload.LastIsNull.Value || DateTimeOffset.TryParse(payload.LastValue, out _)),
            ListTripsCursor.SortField.Title => string.IsNullOrWhiteSpace(payload.LastValue) is false,
            _ => false
        };
    }
}

