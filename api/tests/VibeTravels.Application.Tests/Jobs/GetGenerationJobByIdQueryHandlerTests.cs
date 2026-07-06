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

public sealed class GetGenerationJobByIdQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Handle_ReturnsJob_WhenOwnedByUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var job = CreatePendingJob(UserId, Guid.NewGuid());
        SetProperty(job, nameof(AiGenerationJob.AttemptNo), 2);
        SetProperty(job, nameof(AiGenerationJob.ErrorCode), "AI_TIMEOUT");
        SetProperty(job, nameof(AiGenerationJob.ErrorMessage), "Timed out");
        SetProperty(job, nameof(AiGenerationJob.Discarded), true);
        SetProperty(job, nameof(AiGenerationJob.DiscardReason), "Superseded");

        db.AiGenerationJobs.Add(job);
        await db.SaveChangesAsync();

        var handler = new GetGenerationJobByIdQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(job.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(job.Id, result.Value!.Job.Id);
        Assert.Equal("queued", result.Value.Job.Status.ToString().ToLowerInvariant());
        Assert.Equal(2, result.Value.Job.AttemptNo);
        Assert.Equal("AI_TIMEOUT", result.Value.Job.ErrorCode);
        Assert.Equal("Timed out", result.Value.Job.ErrorMessage);
        Assert.True(result.Value.Job.Discarded);
        Assert.Equal("Superseded", result.Value.Job.DiscardReason);
    }

    [Fact]
    public async Task Handle_ReturnsJobNotFound_WhenJobDoesNotExist()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        await db.SaveChangesAsync();

        var handler = new GetGenerationJobByIdQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(Guid.NewGuid())),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("JOB_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsJobNotFound_WhenJobBelongsToDifferentUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(OtherUserId));

        var job = CreatePendingJob(OtherUserId, Guid.NewGuid());
        db.AiGenerationJobs.Add(job);
        await db.SaveChangesAsync();

        var handler = new GetGenerationJobByIdQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(job.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("JOB_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_MapsRunningStatusToProcessing()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var job = CreatePendingJob(UserId, Guid.NewGuid());
        SetProperty(job, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Running);
        db.AiGenerationJobs.Add(job);
        await db.SaveChangesAsync();

        var handler = new GetGenerationJobByIdQueryHandler(db, new GenerationJobStatusMapper());
        var result = await handler.Handle(
            new GetGenerationJobByIdQuery(UserId, new GetGenerationJobByIdQueryRequest(job.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("processing", result.Value!.Job.Status.ToString().ToLowerInvariant());
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
