using FluentValidation;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .SetValidator(new CreateTripCommandRequestValidator());
    }
}

