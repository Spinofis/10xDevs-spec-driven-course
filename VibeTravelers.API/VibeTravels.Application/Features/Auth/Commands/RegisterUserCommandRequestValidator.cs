using FluentValidation;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed class RegisterUserCommandRequestValidator : AbstractValidator<RegisterUserCommandRequest>
{
    public RegisterUserCommandRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
