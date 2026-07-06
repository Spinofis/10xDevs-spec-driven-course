using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Me.Commands;
using VibeTravels.Application.Features.Me.Commands.Models;
using VibeTravels.Application.Features.Me.Handlers;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Me;

public sealed class UpsertUserProfileCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TagId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

    [Fact]
    public async Task Handle_CreatesProfile_WhenNoneExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new UpsertUserProfileCommandHandler(db);
        var result = await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(BudgetLevel.High, 2, Pace.Fast, "My notes", true),
                [])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var profile = await db.UserProfiles.FindAsync(UserId);
        Assert.NotNull(profile);
        Assert.Equal("High", profile!.DefaultBudgetLevel);
        Assert.Equal(2, profile.DefaultPeopleCount);
        Assert.Equal("Fast", profile.DefaultPace);
        Assert.Equal("My notes", profile.DefaultNotes);
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public async Task Handle_UpdatesProfile_WhenAlreadyExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        var existing = UserProfile.Create(UserId);
        existing.Update("Low", 1, "Relaxed", "Old notes", true);
        db.UserProfiles.Add(existing);
        await db.SaveChangesAsync();

        var handler = new UpsertUserProfileCommandHandler(db);
        var result = await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(BudgetLevel.Medium, 4, Pace.Normal, "New notes", false),
                [])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var profile = await db.UserProfiles.FindAsync(UserId);
        Assert.NotNull(profile);
        Assert.Equal("Medium", profile!.DefaultBudgetLevel);
        Assert.Equal(4, profile.DefaultPeopleCount);
        Assert.Equal("Normal", profile.DefaultPace);
        Assert.Equal("New notes", profile.DefaultNotes);
        Assert.False(profile.IsDefault);
    }

    [Fact]
    public async Task Handle_ReturnsTagNotFound_WhenTagDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new UpsertUserProfileCommandHandler(db);
        var result = await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(null, null, null, null, true),
                [new PreferenceTagCommandModel(TagId, 1)])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TAG_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReplacesPreferenceTags_OnSecondUpsert()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "mountains", "Mountains");
        db.Tags.AddRange(tagA, tagB);
        await db.SaveChangesAsync();

        var handler = new UpsertUserProfileCommandHandler(db);

        await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(null, null, null, null, true),
                [new PreferenceTagCommandModel(tagA.Id, 1), new PreferenceTagCommandModel(tagB.Id, 2)])),
            CancellationToken.None);

        var afterFirst = await db.UserPreferenceTags
            .Where(pt => pt.UserId == UserId)
            .ToListAsync();
        Assert.Equal(2, afterFirst.Count);

        await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(null, null, null, null, true),
                [new PreferenceTagCommandModel(tagB.Id, 1)])),
            CancellationToken.None);

        var afterSecond = await db.UserPreferenceTags
            .Where(pt => pt.UserId == UserId)
            .ToListAsync();
        Assert.Single(afterSecond);
        Assert.Equal(tagB.Id, afterSecond[0].TagId);
        Assert.Equal(1, afterSecond[0].Order);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPreferenceTagsIsEmpty()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new UpsertUserProfileCommandHandler(db);
        var result = await handler.Handle(
            new UpsertUserProfileCommand(UserId, new UpsertUserProfileCommandRequest(
                new UserProfileCommandModel(null, null, null, null, true),
                [])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private static User CreateUser(Guid id)
    {
        var result = User.Create($"{id}@example.com", "pass-hash");
        Assert.True(result.IsSuccess);
        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        return user;
    }

    private static Tag CreateTag(Guid id, string code, string displayName)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;
        typeof(Tag).GetProperty(nameof(Tag.Id))!.SetValue(tag, id);
        typeof(Tag).GetProperty(nameof(Tag.Code))!.SetValue(tag, code);
        typeof(Tag).GetProperty(nameof(Tag.DisplayName))!.SetValue(tag, displayName);
        typeof(Tag).GetProperty(nameof(Tag.CreatedAt))!.SetValue(tag, DateTimeOffset.UtcNow);
        return tag;
    }

    private static TestAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestAppDbContext(options);
    }

    private sealed class TestAppDbContext : DbContext, IAppDbContext
    {
        public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserPreferenceTag> UserPreferenceTags => Set<UserPreferenceTag>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<TripTag> TripTags => Set<TripTag>();
        public DbSet<AiGenerationJob> AiGenerationJobs => Set<AiGenerationJob>();
        public DbSet<TripPlan> TripPlans => Set<TripPlan>();
        public DbSet<PlanItem> PlanItems => Set<PlanItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(builder =>
            {
                builder.Property(e => e.Email).HasConversion(
                    email => email.Value,
                    value => Email.From(value));
                builder.Property(e => e.PasswordHash).HasConversion(
                    password => password.Value,
                    value => Password.From(value));
            });

            modelBuilder.Entity<Trip>(builder =>
            {
                builder.Property(e => e.Title).HasConversion(
                    title => title.Value,
                    value => TripTitle.From(value));
                builder.Property(e => e.PlaceText).HasConversion(
                    place => place == null ? null : place.Value,
                    value => string.IsNullOrWhiteSpace(value) ? null : TripPlaceText.From(value));
            });

            modelBuilder.Entity<UserProfile>(builder => builder.HasKey(e => e.UserId));
            modelBuilder.Entity<TripTag>(builder => builder.HasKey(e => new { e.TripId, e.TagId }));
            modelBuilder.Entity<UserPreferenceTag>(builder => builder.HasKey(e => new { e.UserId, e.TagId }));
            modelBuilder.Entity<TripPlan>(builder => builder.HasKey(e => e.TripId));

            base.OnModelCreating(modelBuilder);
        }
    }
}
