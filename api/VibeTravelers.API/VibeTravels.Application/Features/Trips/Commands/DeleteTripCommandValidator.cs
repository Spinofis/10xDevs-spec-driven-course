using FluentValidation;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed class DeleteTripCommandValidator : AbstractValidator<DeleteTripCommand>
{
    public DeleteTripCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .SetValidator(new DeleteTripCommandRequestValidator());
    }
}

public sealed class DeleteTripCommandRequestValidator : AbstractValidator<DeleteTripCommandRequest>
{
    public DeleteTripCommandRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");
    }
}
