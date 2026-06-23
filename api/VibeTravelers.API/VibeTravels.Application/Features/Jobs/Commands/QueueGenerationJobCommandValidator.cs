using FluentValidation;

namespace VibeTravels.Application.Features.Jobs.Commands;

public sealed class QueueGenerationJobCommandValidator : AbstractValidator<QueueGenerationJobCommand>
{
    public QueueGenerationJobCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new QueueGenerationJobCommandRequestValidator()!);
    }
}

public sealed class QueueGenerationJobCommandRequestValidator : AbstractValidator<QueueGenerationJobCommandRequest>
{
    public QueueGenerationJobCommandRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");
    }
}
