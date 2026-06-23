using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Handlers;
using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Plans;

public sealed class SavePlanCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripDoesNotBelongToUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(OtherUserId));

        var foreignTrip = CreateTrip(OtherUserId);
        db.Trips.Add(foreignTrip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new SavePlanCommand(UserId, new SavePlanCommandRequest(foreignTrip.Id)),
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
            new SavePlanCommand(UserId, new SavePlanCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("PLAN_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsInputChangedSinceGeneration_WhenCurrentTripInputDiffersFromGenerationInput()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var jobResult = AiGenerationJob.CreatePending(
            trip.Id,
            UserId,
            """{"tripId":"123"}""",
            "different-hash",
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero));
        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);

        db.AiGenerationJobs.Add(jobResult.Value!);
        db.TripPlans.Add(TripPlan.Create(
            trip.Id,
            jobResult.Value!.Id,
            title: "Generated plan",
            summary: "Generated summary",
            createdAt: new DateTimeOffset(2026, 8, 1, 9, 5, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new SavePlanCommand(UserId, new SavePlanCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INPUT_CHANGED_SINCE_GENERATION", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_SavesPlan_WhenCurrentTripInputMatchesGenerationInput()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var fingerprintService = new TripInputFingerprintService();
        var fingerprintResult = fingerprintService.Build(trip, UserId);
        Assert.True(fingerprintResult.IsSuccess);
        Assert.NotNull(fingerprintResult.Value);

        var jobResult = AiGenerationJob.CreatePending(
            trip.Id,
            UserId,
            fingerprintResult.Value!.PayloadJson,
            fingerprintResult.Value.Hash,
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero));
        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);

        db.AiGenerationJobs.Add(jobResult.Value!);
        db.TripPlans.Add(TripPlan.Create(
            trip.Id,
            jobResult.Value!.Id,
            title: "Generated plan",
            summary: "Generated summary",
            createdAt: new DateTimeOffset(2026, 8, 1, 9, 5, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new SavePlanCommand(UserId, new SavePlanCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("saved", result.Value!.Result.Status.ToString().ToLowerInvariant());
        Assert.Equal(2, result.Value.Result.Version);
        Assert.Equal(trip.Id, result.Value.Result.TripId);
        Assert.NotEqual(default, result.Value.Result.SavedAt);
    }

    private static SavePlanCommandHandler CreateHandler(IAppDbContext db)
        => new(db, new TripInputFingerprintService());

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
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        return user;
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
