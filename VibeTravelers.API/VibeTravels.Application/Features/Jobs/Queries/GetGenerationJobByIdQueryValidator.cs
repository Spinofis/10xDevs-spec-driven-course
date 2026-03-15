using FluentValidation;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed class GetGenerationJobByIdQueryValidator : AbstractValidator<GetGenerationJobByIdQuery>
{
    public GetGenerationJobByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new GetGenerationJobByIdQueryRequestValidator()!);
    }
}

public sealed class GetGenerationJobByIdQueryRequestValidator : AbstractValidator<GetGenerationJobByIdQueryRequest>
{
    public GetGenerationJobByIdQueryRequestValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}
