using FluentValidation;

namespace VibeTravels.Application.Features.Plans.Queries;

public sealed class GetPlanByTripIdQueryValidator : AbstractValidator<GetPlanByTripIdQuery>
{
    public GetPlanByTripIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new GetPlanByTripIdQueryRequestValidator()!);
    }
}

public sealed class GetPlanByTripIdQueryRequestValidator : AbstractValidator<GetPlanByTripIdQueryRequest>
{
    public GetPlanByTripIdQueryRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");
    }
}
