using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Handlers;
using VibeTravels.Application.Features.Plans.Queries;
using VibeTravels.Application.Features.Plans.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Plans;

public sealed class GetPlanByTripIdQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(Guid.NewGuid())),
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

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsPlanNotFound_WhenTripExistsButPlanIsMissing()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("PLAN_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsMappedPlan_WithStableItemOrdering()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var generationJobId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var createdAt = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var plan = TripPlan.Create(trip.Id, generationJobId, "Summer trip", "City highlights", createdAt);
        db.TripPlans.Add(plan);

        db.PlanItems.Add(PlanItem.Create(
            trip.Id,
            itemDate: new DateOnly(2026, 8, 2),
            itemTime: new TimeOnly(19, 0),
            sortOrder: 20,
            placeType: PlanItemPlaceType.Restaurant,
            placeName: "Dinner",
            description: "Seafood",
            createdAt));

        db.PlanItems.Add(PlanItem.Create(
            trip.Id,
            itemDate: new DateOnly(2026, 8, 1),
            itemTime: new TimeOnly(14, 0),
            sortOrder: 30,
            placeType: PlanItemPlaceType.Attraction,
            placeName: "Museum",
            description: "Old town museum",
            createdAt));

        db.PlanItems.Add(PlanItem.Create(
            trip.Id,
            itemDate: new DateOnly(2026, 8, 1),
            itemTime: new TimeOnly(9, 0),
            sortOrder: 10,
            placeType: PlanItemPlaceType.Restaurant,
            placeName: "Breakfast",
            description: "Cafe",
            createdAt));

        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new GetPlanByTripIdQuery(UserId, new GetPlanByTripIdQueryRequest(trip.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var model = result.Value!.Plan;
        Assert.Equal(trip.Id, model.TripId);
        Assert.Equal(1, model.Version);
        Assert.Equal("generated", model.Status.ToString().ToLowerInvariant());
        Assert.Equal(generationJobId, model.GeneratedFromJobId);
        Assert.NotNull(model.GeneratedAt);
        Assert.NotNull(model.SavedAt);
        Assert.Equal("City highlights", model.Summary);

        Assert.Equal(3, model.Items.Count);
        Assert.Equal("Breakfast", model.Items[0].Title);
        Assert.Equal(1, model.Items[0].DayNumber);
        Assert.Equal(10, model.Items[0].Order);
        Assert.Equal(PlanItemPlaceType.Restaurant, model.Items[0].PlaceType);

        Assert.Equal("Museum", model.Items[1].Title);
        Assert.Equal(1, model.Items[1].DayNumber);
        Assert.Equal(30, model.Items[1].Order);
        Assert.Equal(PlanItemPlaceType.Attraction, model.Items[1].PlaceType);

        Assert.Equal("Dinner", model.Items[2].Title);
        Assert.Equal(2, model.Items[2].DayNumber);
        Assert.Equal(20, model.Items[2].Order);
        Assert.Equal(PlanItemPlaceType.Restaurant, model.Items[2].PlaceType);
    }

    private static GetPlanByTripIdQueryHandler CreateHandler(IAppDbContext db)
    {
        ITripPlanReadService readService = new TripPlanReadService(db);
        return new GetPlanByTripIdQueryHandler(db, readService);
    }

    private static Trip CreateTrip(Guid userId)
    {
        var result = Trip.Create(
            userId,
            title: "Plan trip",
            placeText: "Porto",
            noteText: "Food and architecture",
            dateFrom: new DateOnly(2026, 8, 1),
            dateTo: new DateOnly(2026, 8, 4),
            stayLengthMinDays: 2,
            stayLengthMaxDays: 4,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

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

            modelBuilder.Entity<TripTag>(builder =>
            {
                builder.HasKey(e => new { e.TripId, e.TagId });
            });

            modelBuilder.Entity<TripPlan>(builder =>
            {
                builder.HasKey(e => e.TripId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
