using FluentValidation;
using VibeTravels.Application.Features.Me.Commands.Models;

namespace VibeTravels.Application.Features.Me.Commands;

public sealed class UpsertUserProfileCommandValidator : AbstractValidator<UpsertUserProfileCommand>
{
    public UpsertUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new UpsertUserProfileCommandRequestValidator()!);
    }
}

public sealed class UpsertUserProfileCommandRequestValidator : AbstractValidator<UpsertUserProfileCommandRequest>
{
    public UpsertUserProfileCommandRequestValidator()
    {
        RuleFor(x => x.Profile)
            .NotNull()
            .SetValidator(new UserProfileCommandModelValidator()!);

        RuleFor(x => x.PreferenceTags)
            .NotNull();

        RuleForEach(x => x.PreferenceTags)
            .SetValidator(new PreferenceTagCommandModelValidator());

        RuleFor(x => x.PreferenceTags)
            .Must(tags => tags == null || tags.Select(t => t.TagId).Distinct().Count() == tags.Count)
            .WithMessage("Preference tags must not contain duplicate TagIds.")
            .When(x => x.PreferenceTags != null);
    }
}

public sealed class UserProfileCommandModelValidator : AbstractValidator<UserProfileCommandModel>
{
    public UserProfileCommandModelValidator()
    {
        RuleFor(x => x.DefaultPeopleCount)
            .GreaterThan(0).WithMessage("DefaultPeopleCount must be greater than 0.")
            .When(x => x.DefaultPeopleCount.HasValue);
    }
}

public sealed class PreferenceTagCommandModelValidator : AbstractValidator<PreferenceTagCommandModel>
{
    public PreferenceTagCommandModelValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("TagId is required.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }
}
