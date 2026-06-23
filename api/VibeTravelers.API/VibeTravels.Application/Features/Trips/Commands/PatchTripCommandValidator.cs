using FluentValidation;
using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed class PatchTripCommandValidator : AbstractValidator<PatchTripCommand>
{
    public PatchTripCommandValidator()
    {
        RuleFor(x => x.Request)
            .SetValidator(new PatchTripCommandRequestValidator());
    }
}

public sealed class PatchTripCommandRequestValidator : AbstractValidator<PatchTripCommandRequest>
{
    public PatchTripCommandRequestValidator()
    {
        RuleFor(x => x.ToModel())
            .SetValidator(new PatchTripCommandModelValidator());
    }
}

public sealed class PatchTripCommandModelValidator : AbstractValidator<PatchTripCommandModel>
{
    public PatchTripCommandModelValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldSet)
            .WithMessage("At least one field must be provided.");

        RuleFor(x => x.Title)
            .Must(x => x.IsSet is false || x.Value is not null)
            .WithMessage("Title cannot be null.");

        RuleFor(x => x.Title)
            .Must(x => x.IsSet is false || string.IsNullOrWhiteSpace(x.Value) is false)
            .When(x => x.Title.IsSet && x.Title.Value is not null)
            .WithMessage("Title cannot be empty.");

        RuleFor(x => x.Title)
            .Must(x => x.IsSet is false || x.Value.Length <= TripTitle.MaxLength)
            .When(x => x.Title.IsSet && x.Title.Value is not null)
            .WithMessage($"Title must be at most {TripTitle.MaxLength} characters.");

        RuleFor(x => x.PlaceText)
            .Must(x => x.IsSet is false || x.Value is not null)
            .WithMessage("PlaceText cannot be null.");

        RuleFor(x => x.PlaceText)
            .Must(x => x.IsSet is false || x.Value.Length <= TripPlaceText.MaxLength)
            .When(x => x.PlaceText.IsSet && x.PlaceText.Value is not null)
            .WithMessage($"PlaceText must be at most {TripPlaceText.MaxLength} characters.");

        RuleFor(x => x.NoteText)
            .Must(x => x.IsSet is false || (x.Value?.Length ?? 0) <= Trip.NoteTextMaxLength)
            .WithMessage($"NoteText must be at most {Trip.NoteTextMaxLength} characters.");

        RuleFor(x => x.StayLengthMinDays)
            .Must(x => x.IsSet is false || x.Value > 0)
            .When(x => x.StayLengthMinDays.IsSet)
            .WithMessage("StayLengthMinDays must be greater than 0.");

        RuleFor(x => x.StayLengthMaxDays)
            .Must(x => x.IsSet is false || x.Value > 0)
            .When(x => x.StayLengthMaxDays.IsSet)
            .WithMessage("StayLengthMaxDays must be greater than 0.");

        RuleFor(x => x.PeopleCount)
            .Must(x => x.IsSet is false || x.Value > 0)
            .When(x => x.PeopleCount.IsSet)
            .WithMessage("PeopleCount must be greater than 0.");

        RuleFor(x => x.Tags)
            .Must(x => x.IsSet is false || x.Value is not null)
            .WithMessage("Tags cannot be null.");

        RuleFor(x => x.Tags)
            .Must(x => x.IsSet is false || x.Value!.Select(t => t.TagId).Distinct().Count() == x.Value.Count)
            .When(x => x.Tags.IsSet && x.Tags.Value is not null)
            .WithMessage("Tags contain duplicate TagId values.");

        RuleForEach(x => x.Tags.Value)
            .SetValidator(new TripTagCommandModelValidator())
            .When(x => x.Tags.IsSet && x.Tags.Value is not null);
    }

    private static bool HasAtLeastOneFieldSet(PatchTripCommandModel model)
    {
        return model.Title.IsSet
            || model.PlaceText.IsSet
            || model.NoteText.IsSet
            || model.DateFrom.IsSet
            || model.DateTo.IsSet
            || model.StayLengthMinDays.IsSet
            || model.StayLengthMaxDays.IsSet
            || model.PeopleCount.IsSet
            || model.BudgetLevel.IsSet
            || model.Pace.IsSet
            || model.Tags.IsSet;
    }
}
