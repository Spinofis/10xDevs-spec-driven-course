using FluentValidation;
using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Features.Plans.Commands;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new UpdatePlanCommandRequestValidator()!);
    }
}

public sealed class UpdatePlanCommandRequestValidator : AbstractValidator<UpdatePlanCommandRequest>
{
    public UpdatePlanCommandRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("TripId is required.");

        RuleFor(x => x.ToModel())
            .SetValidator(new UpdatePlanCommandModelValidator());
    }
}

public sealed class UpdatePlanCommandModelValidator : AbstractValidator<UpdatePlanCommandModel>
{
    public UpdatePlanCommandModelValidator()
    {
        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("Items are required.");

        RuleFor(x => x.Items)
            .Must(items => items is { Count: > 0 })
            .When(x => x.Items is not null)
            .WithMessage("Items cannot be empty.");

        RuleFor(x => x.Items)
            .Must(HaveUniqueIds)
            .When(x => x.Items is not null)
            .WithMessage("Items contain duplicate Id values.");

        RuleForEach(x => x.Items)
            .SetValidator(new PlanItemCommandModelValidator());
    }

    private static bool HaveUniqueIds(IReadOnlyList<PlanItemCommandModel> items)
        => items.Select(x => x.Id).Distinct().Count() == items.Count;
}

public sealed class PlanItemCommandModelValidator : AbstractValidator<PlanItemCommandModel>
{
    public PlanItemCommandModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Item Id is required.");

        RuleFor(x => x.DayNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("DayNumber must be greater than or equal to 1.");

        RuleFor(x => x.ItemDate)
            .Must(x => x != default)
            .WithMessage("ItemDate is required.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Order must be greater than or equal to 0.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.");

        RuleFor(x => x.Title)
            .Must(title => string.IsNullOrWhiteSpace(title) is false)
            .WithMessage("Title is required.");

        RuleFor(x => x.CreatedAt)
            .Must(x => x != default)
            .WithMessage("CreatedAt is required.");

        RuleFor(x => x.UpdatedAt)
            .Must(x => x != default)
            .WithMessage("UpdatedAt is required.");

        RuleFor(x => x.PlaceType)
            .Must(BeSupportedPlaceType)
            .WithMessage("PlaceType must be Attraction, Restaurant, or Hotel.");

        RuleFor(x => x)
            .Must(x => x.CreatedAt <= x.UpdatedAt)
            .WithMessage("CreatedAt must be less than or equal to UpdatedAt.");
    }

    private static bool BeSupportedPlaceType(PlanItemPlaceType placeType)
        => placeType is PlanItemPlaceType.Attraction or PlanItemPlaceType.Restaurant or PlanItemPlaceType.Hotel;
}
