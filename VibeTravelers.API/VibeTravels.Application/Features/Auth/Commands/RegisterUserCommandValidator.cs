using FluentValidation;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Request)
            .SetValidator(new RegisterUserCommandRequestValidator());
    }
}

