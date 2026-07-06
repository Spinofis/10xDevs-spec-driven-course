using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Me.Handlers;
using VibeTravels.Application.Features.Me.Queries;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Me;

public sealed class GetUserProfileQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_ReturnsDefaultProfile_WhenNoProfileExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new GetUserProfileQueryHandler(db);
        var result = await handler.Handle(
            new GetUserProfileQuery(UserId, new GetUserProfileQueryRequest()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var response = result.Value!;
        Assert.Equal(UserId, response.UserId);
        Assert.Null(response.Profile.DefaultBudgetLevel);
        Assert.Null(response.Profile.DefaultPeopleCount);
        Assert.Null(response.Profile.DefaultPace);
        Assert.Null(response.Profile.DefaultNotes);
        Assert.True(response.Profile.IsDefault);
        Assert.Empty(response.PreferenceTags);
    }

    [Fact]
    public async Task Handle_ReturnsStoredProfile_WhenProfileExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var profile = UserProfile.Create(UserId);
        profile.Update("Medium", 3, "Normal", "Some notes", false);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetUserProfileQueryHandler(db);
        var result = await handler.Handle(
            new GetUserProfileQuery(UserId, new GetUserProfileQueryRequest()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var response = result.Value!;

        Assert.Equal(VibeTravels.Application.Features.Common.BudgetLevel.Medium, response.Profile.DefaultBudgetLevel);
        Assert.Equal(3, response.Profile.DefaultPeopleCount);
        Assert.Equal(VibeTravels.Application.Features.Common.Pace.Normal, response.Profile.DefaultPace);
        Assert.Equal("Some notes", response.Profile.DefaultNotes);
        Assert.False(response.Profile.IsDefault);
    }

    [Fact]
    public async Task Handle_ReturnsPreferenceTags_OrderedByOrderThenTagId()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "mountains", "Mountains");
        db.Tags.AddRange(tagA, tagB);

        var profile = UserProfile.Create(UserId);
        db.UserProfiles.Add(profile);

        db.UserPreferenceTags.Add(UserPreferenceTag.Create(UserId, tagB.Id, 2));
        db.UserPreferenceTags.Add(UserPreferenceTag.Create(UserId, tagA.Id, 1));
        await db.SaveChangesAsync();

        var handler = new GetUserProfileQueryHandler(db);
        var result = await handler.Handle(
            new GetUserProfileQuery(UserId, new GetUserProfileQueryRequest()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tags = result.Value!.PreferenceTags;
        Assert.Equal(2, tags.Count);
        Assert.Equal("beach", tags[0].Tag.Code);
        Assert.Equal(1, tags[0].Order);
        Assert.Equal("mountains", tags[1].Tag.Code);
        Assert.Equal(2, tags[1].Order);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyTags_WhenProfileExistsButNoTagsSet()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.UserProfiles.Add(UserProfile.Create(UserId));
        await db.SaveChangesAsync();

        var handler = new GetUserProfileQueryHandler(db);
        var result = await handler.Handle(
            new GetUserProfileQuery(UserId, new GetUserProfileQueryRequest()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.PreferenceTags);
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
