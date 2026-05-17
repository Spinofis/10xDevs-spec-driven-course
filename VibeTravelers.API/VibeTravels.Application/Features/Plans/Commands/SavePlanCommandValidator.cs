using FluentValidation;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed class SavePlanCommandValidator : AbstractValidator<SavePlanCommand>
{
    public SavePlanCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new SavePlanCommandRequestValidator()!);
    }
}

public sealed class SavePlanCommandRequestValidator : AbstractValidator<SavePlanCommandRequest>
{
    public SavePlanCommandRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required.");
    }
}
