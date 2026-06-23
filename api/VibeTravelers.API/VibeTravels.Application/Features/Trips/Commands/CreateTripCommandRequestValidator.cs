using FluentValidation;
using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed class CreateTripCommandRequestValidator : AbstractValidator<CreateTripCommandRequest>
{
    public CreateTripCommandRequestValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .SetValidator(new CreateTripCommandModelValidator()!);
    }
}

public sealed class CreateTripCommandModelValidator : AbstractValidator<CreateTripCommandModel>
{
    public CreateTripCommandModelValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TripTitle.MaxLength);

        RuleFor(x => x.PlaceText)
            .MaximumLength(TripPlaceText.MaxLength)
            .When(x => string.IsNullOrWhiteSpace(x.PlaceText) is false);

        RuleFor(x => x.NoteText)
            .MaximumLength(Trip.NoteTextMaxLength)
            .When(x => x.NoteText is not null);

        RuleFor(x => x.DateFrom)
            .NotNull().WithMessage("DateFrom is required.");

        RuleFor(x => x.DateTo)
            .NotNull().WithMessage("DateTo is required.");

        RuleFor(x => x)
            .Must(x => x.DateFrom is null || x.DateTo is null || x.DateTo >= x.DateFrom)
            .WithMessage("DateTo must be greater than or equal to DateFrom.");

        RuleFor(x => x.StayLengthMinDays)
            .NotNull().WithMessage("StayLengthMinDays is required.")
            .GreaterThan(0).When(x => x.StayLengthMinDays is not null);

        RuleFor(x => x.StayLengthMaxDays)
            .NotNull().WithMessage("StayLengthMaxDays is required.")
            .GreaterThan(0).When(x => x.StayLengthMaxDays is not null);

        RuleFor(x => x)
            .Must(x =>
                x.StayLengthMinDays is null
                || x.StayLengthMaxDays is null
                || x.StayLengthMaxDays >= x.StayLengthMinDays)
            .WithMessage("StayLengthMaxDays must be greater than or equal to StayLengthMinDays.");

        RuleFor(x => x.PeopleCount)
            .NotNull().WithMessage("PeopleCount is required.")
            .GreaterThan(0).When(x => x.PeopleCount is not null);

        RuleForEach(x => x.Tags)
            .SetValidator(new TripTagCommandModelValidator()!);
    }
}

public sealed class TripTagCommandModelValidator : AbstractValidator<TripTagCommandModel>
{
    public TripTagCommandModelValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("TagId is required.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Order is not null);
    }
}
