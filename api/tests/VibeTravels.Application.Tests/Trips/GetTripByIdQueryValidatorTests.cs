using VibeTravels.Application.Features.Trips.Queries;

namespace VibeTravels.Application.Tests.Trips;

public sealed class GetTripByIdQueryValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Validate_Fails_WhenUserIdIsEmpty()
    {
        var validator = new GetTripByIdQueryValidator();
        var query = new GetTripByIdQuery(Guid.Empty, new GetTripByIdQueryRequest(Guid.NewGuid()));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenTripIdIsEmpty()
    {
        var validator = new GetTripByIdQueryValidator();
        var query = new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(Guid.Empty));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenRequestIsValid()
    {
        var validator = new GetTripByIdQueryValidator();
        var query = new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(Guid.NewGuid()));

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
