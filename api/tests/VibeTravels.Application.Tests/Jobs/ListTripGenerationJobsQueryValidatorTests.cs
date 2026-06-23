using VibeTravels.Application.Features.Jobs.Queries;

namespace VibeTravels.Application.Tests.Jobs;

public sealed class ListTripGenerationJobsQueryValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TripId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Validate_Fails_WhenTripIdIsEmpty()
    {
        var validator = new ListTripGenerationJobsQueryValidator();
        var query = new ListTripGenerationJobsQuery(
            UserId,
            new ListTripGenerationJobsQueryRequest(Guid.Empty, Limit: 20, Cursor: null));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenLimitIsTooLarge()
    {
        var validator = new ListTripGenerationJobsQueryValidator();
        var query = new ListTripGenerationJobsQuery(
            UserId,
            new ListTripGenerationJobsQueryRequest(TripId, Limit: 101, Cursor: null));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenCursorIsInvalid()
    {
        var validator = new ListTripGenerationJobsQueryValidator();
        var query = new ListTripGenerationJobsQuery(
            UserId,
            new ListTripGenerationJobsQueryRequest(TripId, Limit: 20, Cursor: "not-a-valid-cursor"));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenRequestIsValid()
    {
        var validator = new ListTripGenerationJobsQueryValidator();
        var query = new ListTripGenerationJobsQuery(
            UserId,
            new ListTripGenerationJobsQueryRequest(TripId, Limit: 20, Cursor: null));

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
