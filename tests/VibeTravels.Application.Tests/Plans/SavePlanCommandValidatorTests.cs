using FluentValidation.TestHelper;
using VibeTravels.Application.Features.Plans.Commands;

namespace VibeTravels.Application.Tests.Plans;

public sealed class SavePlanCommandValidatorTests
{
    private readonly SavePlanCommandValidator _validator = new();

    [Fact]
    public void Validate_ReturnsError_WhenUserIdIsEmpty()
    {
        var command = new SavePlanCommand(Guid.Empty, new SavePlanCommandRequest(Guid.NewGuid()));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_ReturnsError_WhenTripIdIsEmpty()
    {
        var command = new SavePlanCommand(Guid.NewGuid(), new SavePlanCommandRequest(Guid.Empty));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Request.TripId");
    }

    [Fact]
    public void Validate_ReturnsNoErrors_WhenCommandIsValid()
    {
        var command = new SavePlanCommand(Guid.NewGuid(), new SavePlanCommandRequest(Guid.NewGuid()));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
