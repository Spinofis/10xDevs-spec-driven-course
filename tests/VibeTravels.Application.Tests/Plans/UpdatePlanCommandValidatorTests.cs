using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Domain.Entities.Plans;

namespace VibeTravels.Application.Tests.Plans;

public sealed class UpdatePlanCommandValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TripId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Validate_Fails_WhenItemsAreEmpty()
    {
        var validator = new UpdatePlanCommandValidator();
        var command = new UpdatePlanCommand(UserId, new UpdatePlanCommandRequest(TripId, "summary", Array.Empty<PlanItemCommandModel>()));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Items cannot be empty."));
    }

    [Fact]
    public void Validate_Fails_WhenItemIdsAreDuplicated()
    {
        var validator = new UpdatePlanCommandValidator();
        var duplicatedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var items = new[]
        {
            CreateItem(duplicatedId, "Breakfast"),
            CreateItem(duplicatedId, "Museum")
        };

        var command = new UpdatePlanCommand(UserId, new UpdatePlanCommandRequest(TripId, "summary", items));
        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("duplicate Id"));
    }

    [Fact]
    public void Validate_Fails_WhenCreatedAtIsAfterUpdatedAt()
    {
        var validator = new UpdatePlanCommandValidator();
        var itemId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var command = new UpdatePlanCommand(UserId, new UpdatePlanCommandRequest(
            TripId,
            "summary",
            new[]
            {
                new PlanItemCommandModel(
                    itemId,
                    DayNumber: 1,
                    ItemDate: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                    Order: 10,
                    Title: "Breakfast",
                    Description: null,
                    LocationText: "Cafe",
                    CreatedAt: new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero),
                    UpdatedAt: new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    PlaceType: PlanItemPlaceType.Restaurant)
            }));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("CreatedAt must be less than or equal to UpdatedAt."));
    }

    [Fact]
    public void Validate_Succeeds_ForValidPayload()
    {
        var validator = new UpdatePlanCommandValidator();
        var command = new UpdatePlanCommand(UserId, new UpdatePlanCommandRequest(
            TripId,
            "summary",
            new[]
            {
                CreateItem(Guid.NewGuid(), "Breakfast"),
                CreateItem(Guid.NewGuid(), "Museum")
            }));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    private static PlanItemCommandModel CreateItem(Guid id, string title)
    {
        return new PlanItemCommandModel(
            id,
            DayNumber: 1,
            ItemDate: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            Order: 10,
            Title: title,
            Description: null,
            LocationText: title,
            CreatedAt: new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            UpdatedAt: new DateTimeOffset(2026, 8, 1, 9, 15, 0, TimeSpan.Zero),
            PlaceType: PlanItemPlaceType.Restaurant);
    }
}
