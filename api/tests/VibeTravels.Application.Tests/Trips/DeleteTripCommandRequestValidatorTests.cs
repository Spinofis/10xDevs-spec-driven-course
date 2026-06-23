using VibeTravels.Application.Features.Trips.Commands;

namespace VibeTravels.Application.Tests.Trips;

public sealed class DeleteTripCommandRequestValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Validate_Fails_WhenTripIdIsEmpty()
    {
        var validator = new DeleteTripCommandValidator();
        var command = new DeleteTripCommand(UserId, new DeleteTripCommandRequest(Guid.Empty));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenTripIdIsPresent()
    {
        var validator = new DeleteTripCommandValidator();
        var command = new DeleteTripCommand(UserId, new DeleteTripCommandRequest(Guid.NewGuid()));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
