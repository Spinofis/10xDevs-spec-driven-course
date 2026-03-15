using VibeTravels.Application.Features.Jobs.Queries;

namespace VibeTravels.Application.Tests.Jobs;

public sealed class GetGenerationJobByIdQueryValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Validate_Fails_WhenJobIdIsEmpty()
    {
        var validator = new GetGenerationJobByIdQueryValidator();
        var query = new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(Guid.Empty));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenJobIdIsPresent()
    {
        var validator = new GetGenerationJobByIdQueryValidator();
        var query = new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(Guid.NewGuid()));

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
