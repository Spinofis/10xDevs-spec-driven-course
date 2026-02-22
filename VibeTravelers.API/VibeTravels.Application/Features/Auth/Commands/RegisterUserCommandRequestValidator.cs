using FluentValidation;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Features.Auth.Commands;

public sealed class RegisterUserCommandRequestValidator : AbstractValidator<RegisterUserCommandRequest>
{
    public RegisterUserCommandRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(Email.MaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(Password.MaxLength);
    }
}
