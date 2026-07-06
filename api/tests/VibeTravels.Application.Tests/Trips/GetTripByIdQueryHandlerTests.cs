using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Trips.Handlers;
using VibeTravels.Application.Features.Trips.Queries;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Trips;

public sealed class GetTripByIdQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new GetTripByIdQueryHandler(db);
        var result = await handler.Handle(
            new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(Guid.NewGuid())),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripBelongsToDifferentUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(OtherUserId));

        var trip = CreateTrip(OtherUserId);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new GetTripByIdQueryHandler(db);
        var result = await handler.Handle(
            new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripIsSoftDeleted()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        var deleteResult = trip.SoftDelete(DateTimeOffset.UtcNow);
        Assert.True(deleteResult.IsSuccess);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new GetTripByIdQueryHandler(db);
        var result = await handler.Handle(
            new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsTripWithTags_OrderedByOrderThenTagId()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "museum", "Museum");

        db.Trips.Add(trip);
        db.Tags.AddRange(tagA, tagB);
        db.TripTags.Add(TripTag.Create(trip.Id, tagB.Id, 2).Value!);
        db.TripTags.Add(TripTag.Create(trip.Id, tagA.Id, 1).Value!);
        await db.SaveChangesAsync();

        var handler = new GetTripByIdQueryHandler(db);
        var result = await handler.Handle(
            new GetTripByIdQuery(UserId, new GetTripByIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var response = result.Value!;
        Assert.Equal(trip.Id, response.Trip.Id);
        Assert.Equal(UserId, response.Trip.UserId);
        Assert.Equal("Trip title", response.Trip.Title);
        Assert.Equal("Paris", response.Trip.PlaceText);
        Assert.Equal("Trip note", response.Trip.NoteText);
        Assert.Equal(2, response.Trip.PeopleCount);

        Assert.Equal(2, response.Tags.Count);
        Assert.Equal("beach", response.Tags[0].Tag.Code);
        Assert.Equal(1, response.Tags[0].Order);
        Assert.Equal("museum", response.Tags[1].Tag.Code);
        Assert.Equal(2, response.Tags[1].Order);
    }

    private static Trip CreateTrip(Guid userId)
    {
        var result = Trip.Create(
            userId,
            title: "Trip title",
            placeText: "Paris",
            noteText: "Trip note",
            dateFrom: new DateOnly(2026, 5, 1),
            dateTo: new DateOnly(2026, 5, 7),
            stayLengthMinDays: 3,
            stayLengthMaxDays: 7,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: true);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        return result.Value!;
    }

    private static User CreateUser(Guid id)
    {
        var result = User.Create($"{id}@example.com", "pass-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        SetProperty(user, nameof(User.Id), id);
        return user;
    }

    private static Tag CreateTag(Guid id, string code, string displayName)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;
        SetProperty(tag, nameof(Tag.Id), id);
        SetProperty(tag, nameof(Tag.Code), code);
        SetProperty(tag, nameof(Tag.DisplayName), displayName);
        SetProperty(tag, nameof(Tag.CreatedAt), DateTimeOffset.UtcNow);
        return tag;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
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
        public TestAppDbContext(DbContextOptions<TestAppDbContext> options)
            : base(options)
        {
        }

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
