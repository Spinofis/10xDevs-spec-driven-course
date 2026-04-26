using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Commands.Models;
using VibeTravels.Application.Features.Plans.Handlers;
using VibeTravels.Application.Features.Plans.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Plans;

public sealed class UpdatePlanCommandHandlerTests
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
            new UpdatePlanCommand(UserId, CreateRequest(Guid.NewGuid(), "summary")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsPlanNotFound_WhenPlanDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new UpdatePlanCommand(UserId, CreateRequest(trip.Id, "summary")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("PLAN_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsJobAlreadyActive_WhenPendingGenerationJobExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);
        db.TripPlans.Add(TripPlan.Create(
            trip.Id,
            generationJobId: null,
            title: "Trip plan",
            summary: "summary",
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero)));

        var jobResult = AiGenerationJob.CreatePending(
            tripId: trip.Id,
            userId: UserId,
            inputSnapshot: "{}",
            inputHash: "hash",
            requestedAt: new DateTimeOffset(2026, 8, 2, 9, 0, 0, TimeSpan.Zero));
        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);
        db.AiGenerationJobs.Add(jobResult.Value!);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new UpdatePlanCommand(UserId, CreateRequest(trip.Id, "summary")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("JOB_ALREADY_ACTIVE", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReplacesItemsAndSetsSavedStatus_WhenPayloadIsValid()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(OtherUserId));
        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var createdAt = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        db.TripPlans.Add(TripPlan.Create(
            trip.Id,
            generationJobId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            title: "Trip plan",
            summary: "Generated summary",
            createdAt));

        db.PlanItems.Add(PlanItem.CreateGenerated(
            trip.Id,
            dayNumber: 1,
            itemDate: new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero),
            sortOrder: 10,
            placeType: PlanItemPlaceType.Restaurant,
            title: "Old breakfast",
            description: "Old desc",
            locationText: "Old location",
            createdAt));

        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var request = CreateRequest(trip.Id, "Manual summary");
        var result = await handler.Handle(new UpdatePlanCommand(UserId, request), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("saved", result.Value!.Plan.Status.ToString().ToLowerInvariant());
        Assert.Equal(2, result.Value.Plan.Version);
        Assert.Equal("Manual summary", result.Value.Plan.Summary);
        Assert.Single(result.Value.Plan.Items);
        Assert.Equal("Breakfast", result.Value.Plan.Items[0].Title);
        Assert.Equal(1, result.Value.Plan.Items[0].DayNumber);
    }

    private static UpdatePlanCommandHandler CreateHandler(IAppDbContext db)
    {
        ITripPlanReadService readService = new TripPlanReadService(db);
        ITripPlanWriteService writeService = new TripPlanWriteService(db);
        return new UpdatePlanCommandHandler(db, readService, writeService);
    }

    private static UpdatePlanCommandRequest CreateRequest(Guid tripId, string? summary)
    {
        return new UpdatePlanCommandRequest(
            tripId,
            summary,
            new[]
            {
                new PlanItemCommandModel(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    DayNumber: 1,
                    ItemDate: new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
                    Order: 10,
                    Title: "Breakfast",
                    Description: "Cafe stop",
                    LocationText: "Central cafe",
                    CreatedAt: new DateTimeOffset(2026, 8, 9, 9, 0, 0, TimeSpan.Zero),
                    UpdatedAt: new DateTimeOffset(2026, 8, 10, 9, 15, 0, TimeSpan.Zero),
                    PlaceType: PlanItemPlaceType.Restaurant)
            });
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
