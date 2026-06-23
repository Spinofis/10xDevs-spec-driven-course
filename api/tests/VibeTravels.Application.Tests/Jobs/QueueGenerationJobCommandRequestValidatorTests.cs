using VibeTravels.Application.Features.Jobs.Commands;

namespace VibeTravels.Application.Tests.Jobs;

public sealed class QueueGenerationJobCommandRequestValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Validate_Fails_WhenTripIdIsEmpty()
    {
        var validator = new QueueGenerationJobCommandValidator();
        var command = new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(Guid.Empty));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenTripIdIsPresent()
    {
        var validator = new QueueGenerationJobCommandValidator();
        var command = new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(Guid.NewGuid()));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
