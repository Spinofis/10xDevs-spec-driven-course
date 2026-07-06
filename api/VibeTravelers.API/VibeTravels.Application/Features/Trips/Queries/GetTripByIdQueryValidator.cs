using FluentValidation;

namespace VibeTravels.Application.Features.Trips.Queries;

public sealed class GetTripByIdQueryValidator : AbstractValidator<GetTripByIdQuery>
{
    public GetTripByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new GetTripByIdQueryRequestValidator()!);
    }
}

public sealed class GetTripByIdQueryRequestValidator : AbstractValidator<GetTripByIdQueryRequest>
{
    public GetTripByIdQueryRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");
    }
}
