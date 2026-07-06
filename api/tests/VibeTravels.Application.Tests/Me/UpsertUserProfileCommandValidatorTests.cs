using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Me.Commands;
using VibeTravels.Application.Features.Me.Commands.Models;

namespace VibeTravels.Application.Tests.Me;

public sealed class UpsertUserProfileCommandValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static UpsertUserProfileCommand ValidCommand(
        int? peopleCount = 2,
        IReadOnlyList<PreferenceTagCommandModel>? tags = null) =>
        new(UserId, new UpsertUserProfileCommandRequest(
            new UserProfileCommandModel(BudgetLevel.Medium, peopleCount, Pace.Normal, null, true),
            tags ?? []));

    [Fact]
    public void Validate_Succeeds_WhenRequestIsValid()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenAllProfileFieldsAreNull()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var command = new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
            new UserProfileCommandModel(null, null, null, null, true),
            []));
        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenUserIdIsEmpty()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var command = new UpsertUserProfileCommand(Guid.Empty, new UpsertUserProfileCommandRequest(
            new UserProfileCommandModel(null, null, null, null, true),
            []));
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenDefaultPeopleCountIsZero()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand(peopleCount: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenDefaultPeopleCountIsNegative()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand(peopleCount: -1));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_WhenPreferenceTagsContainsValidEntries()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand(tags:
        [
            new PreferenceTagCommandModel(Guid.NewGuid(), 0),
            new PreferenceTagCommandModel(Guid.NewGuid(), 1)
        ]));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenPreferenceTagHasDuplicateTagIds()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var duplicateId = Guid.NewGuid();
        var result = validator.Validate(ValidCommand(tags:
        [
            new PreferenceTagCommandModel(duplicateId, 1),
            new PreferenceTagCommandModel(duplicateId, 2)
        ]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenPreferenceTagHasEmptyTagId()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand(tags:
        [
            new PreferenceTagCommandModel(Guid.Empty, 1)
        ]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenPreferenceTagOrderIsNegative()
    {
        var validator = new UpsertUserProfileCommandValidator();
        var result = validator.Validate(ValidCommand(tags:
        [
            new PreferenceTagCommandModel(Guid.NewGuid(), -1)
        ]));
        Assert.False(result.IsValid);
    }
}
