using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Jobs.Handlers;
using VibeTravels.Application.Features.Jobs.Queries;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Jobs;

public sealed class ListTripGenerationJobsQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new ListTripGenerationJobsQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(Guid.NewGuid(), Limit: 20, Cursor: null)),
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

        var foreignTrip = CreateTrip(OtherUserId);
        db.Trips.Add(foreignTrip);
        await db.SaveChangesAsync();

        var handler = new ListTripGenerationJobsQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(foreignTrip.Id, Limit: 20, Cursor: null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsItemsSortedByRequestedAtDescending()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var older = CreatePendingJob(UserId, trip.Id);
        SetProperty(older, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero));

        var newer = CreatePendingJob(UserId, trip.Id);
        SetProperty(newer, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero));

        db.AiGenerationJobs.AddRange(older, newer);
        await db.SaveChangesAsync();

        var handler = new ListTripGenerationJobsQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(trip.Id, Limit: 20, Cursor: null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(newer.Id, result.Value.Items[0].Id);
        Assert.Equal(older.Id, result.Value.Items[1].Id);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_ComputesNextCursor_AndAllowsSecondPageFetch()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var first = CreatePendingJob(UserId, trip.Id);
        SetProperty(first, nameof(AiGenerationJob.Id), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"));
        SetProperty(first, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero));

        var second = CreatePendingJob(UserId, trip.Id);
        SetProperty(second, nameof(AiGenerationJob.Id), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"));
        SetProperty(second, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero));

        var third = CreatePendingJob(UserId, trip.Id);
        SetProperty(third, nameof(AiGenerationJob.Id), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"));
        SetProperty(third, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero));

        db.AiGenerationJobs.AddRange(first, second, third);
        await db.SaveChangesAsync();

        var handler = new ListTripGenerationJobsQueryHandler(db, new GenerationJobStatusMapper());

        var page1 = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(trip.Id, Limit: 2, Cursor: null)),
            CancellationToken.None);

        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value);
        Assert.Equal(2, page1.Value!.Items.Count);
        Assert.NotNull(page1.Value.NextCursor);
        Assert.Equal(first.Id, page1.Value.Items[0].Id);
        Assert.Equal(second.Id, page1.Value.Items[1].Id);

        var page2 = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(trip.Id, Limit: 2, Cursor: page1.Value.NextCursor)),
            CancellationToken.None);

        Assert.True(page2.IsSuccess);
        Assert.NotNull(page2.Value);
        Assert.Single(page2.Value!.Items);
        Assert.Equal(third.Id, page2.Value.Items[0].Id);
        Assert.Null(page2.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_MapsRunningStatusToProcessing()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);

        var running = CreatePendingJob(UserId, trip.Id);
        SetProperty(running, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Running);
        db.AiGenerationJobs.Add(running);
        await db.SaveChangesAsync();

        var handler = new ListTripGenerationJobsQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new ListTripGenerationJobsQuery(
                UserId,
                new ListTripGenerationJobsQueryRequest(trip.Id, Limit: 20, Cursor: null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!.Items);
        Assert.Equal("processing", result.Value.Items[0].Status.ToString().ToLowerInvariant());
    }

    private static Trip CreateTrip(Guid userId)
    {
        var createResult = Trip.Create(
            userId,
            title: "Trip",
            placeText: "Rome",
            noteText: null,
            dateFrom: new DateOnly(2026, 5, 1),
            dateTo: new DateOnly(2026, 5, 3),
            stayLengthMinDays: 2,
            stayLengthMaxDays: 3,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);
        return createResult.Value!;
    }

    private static AiGenerationJob CreatePendingJob(Guid userId, Guid tripId)
    {
        var result = AiGenerationJob.CreatePending(
            tripId,
            userId,
            """{"tripId":"123"}""",
            "hash",
            DateTimeOffset.UtcNow);

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

            modelBuilder.Entity<TripTag>(builder =>
            {
                builder.HasKey(e => new { e.TripId, e.TagId });
            });

            modelBuilder.Entity<UserPreferenceTag>(builder =>
            {
                builder.HasKey(e => new { e.UserId, e.TagId });
            });

            modelBuilder.Entity<TripPlan>(builder =>
            {
                builder.HasKey(e => e.TripId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
