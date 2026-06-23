using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Jobs.Commands;
using VibeTravels.Application.Features.Jobs.Handlers;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Jobs;

public sealed class QueueGenerationJobCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_QueuesJobAndSnapshot_WhenTripIsReady()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId, stayMin: 2, stayMax: 5);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("queued", result.Value!.Job.Status.ToString().ToLowerInvariant());

        Assert.Equal(1, await db.AiGenerationJobs.CountAsync());
    }

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripBelongsToDifferentUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(Guid.Parse("22222222-2222-2222-2222-222222222222")));

        var trip = CreateTrip(Guid.Parse("22222222-2222-2222-2222-222222222222"), stayMin: 2, stayMax: 5);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsGenerationRequirementsNotMet_WhenStayLengthIsOutOfRange()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId, stayMin: 1, stayMax: 1);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("GENERATION_REQUIREMENTS_NOT_MET", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsJobAlreadyActive_WhenQueuedJobExists()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId, stayMin: 2, stayMax: 5);
        db.Trips.Add(trip);

        var existingJobResult = AiGenerationJob.CreatePending(
            trip.Id,
            UserId,
            """{"tripId":"123"}""",
            "hash",
            DateTimeOffset.UtcNow);
        Assert.True(existingJobResult.IsSuccess);

        db.AiGenerationJobs.Add(existingJobResult.Value!);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(
            new QueueGenerationJobCommand(UserId, new QueueGenerationJobCommandRequest(trip.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("JOB_ALREADY_ACTIVE", result.Errors[0].Code);
    }

    private static Trip CreateTrip(Guid userId, int stayMin, int stayMax)
    {
        var createResult = Trip.Create(
            userId,
            title: "Trip",
            placeText: "Rome",
            noteText: null,
            dateFrom: new DateOnly(2026, 5, 1),
            dateTo: new DateOnly(2026, 5, 3),
            stayLengthMinDays: stayMin,
            stayLengthMaxDays: stayMax,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);
        return createResult.Value!;
    }

    private static QueueGenerationJobCommandHandler CreateHandler(IAppDbContext db)
        => new(
            db,
            new GenerationJobStatusMapper(),
            new TripInputFingerprintService());

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
