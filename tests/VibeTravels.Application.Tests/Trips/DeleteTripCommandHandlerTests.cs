using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Application.Features.Trips.Handlers;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Trips;

public sealed class DeleteTripCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_SoftDeletesTrip_WhenTripExistsForUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new DeleteTripCommandHandler(db);
        var command = new DeleteTripCommand(UserId, new DeleteTripCommandRequest(trip.Id));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await db.Trips.SingleAsync(x => x.Id == trip.Id);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripBelongsToDifferentUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));
        db.Users.Add(CreateUser(Guid.Parse("22222222-2222-2222-2222-222222222222")));

        var trip = CreateTrip(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new DeleteTripCommandHandler(db);
        var command = new DeleteTripCommand(UserId, new DeleteTripCommandRequest(trip.Id));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ReturnsTripNotFound_WhenTripAlreadyDeleted()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateUser(UserId));

        var trip = CreateTrip(UserId);
        var deleteResult = trip.SoftDelete(DateTimeOffset.UtcNow);
        Assert.True(deleteResult.IsSuccess);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new DeleteTripCommandHandler(db);
        var command = new DeleteTripCommand(UserId, new DeleteTripCommandRequest(trip.Id));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TRIP_NOT_FOUND", result.Errors[0].Code);
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

            base.OnModelCreating(modelBuilder);
        }
    }
}
