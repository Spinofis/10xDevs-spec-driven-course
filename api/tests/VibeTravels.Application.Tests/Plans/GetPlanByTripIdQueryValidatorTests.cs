using VibeTravels.Application.Features.Plans.Queries;

namespace VibeTravels.Application.Tests.Plans;

public sealed class GetPlanByTripIdQueryValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Validate_Fails_WhenTripIdIsEmpty()
    {
        var validator = new GetPlanByTripIdQueryValidator();
        var query = new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(Guid.Empty));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenTripIdIsPresent()
    {
        var validator = new GetPlanByTripIdQueryValidator();
        var query = new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(Guid.NewGuid()));

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
