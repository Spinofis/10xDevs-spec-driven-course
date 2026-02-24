using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Tags.Handlers;
using VibeTravels.Application.Features.Tags.Queries;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Application.Tests.Tags;

public sealed class ListTagsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoTagsExist()
    {
        await using var db = CreateDbContext();

        var handler = new ListTagsQueryHandler(db);

        var response = await handler.Handle(new ListTagsQuery(new ListTagsQueryRequest()), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task Handle_ReturnsTagsOrderedByCodeThenId()
    {
        await using var db = CreateDbContext();

        var tagB = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "b", "B", DateTimeOffset.UtcNow);
        var tagA2 = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "a", "A2", DateTimeOffset.UtcNow);
        var tagA1 = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa0"), "a", "A1", DateTimeOffset.UtcNow);

        db.Tags.AddRange(tagB, tagA2, tagA1);
        await db.SaveChangesAsync();

        var handler = new ListTagsQueryHandler(db);

        var response = await handler.Handle(new ListTagsQuery(new ListTagsQueryRequest()), CancellationToken.None);

        Assert.Equal(3, response.Items.Count);
        Assert.Equal("a", response.Items[0].Code);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa0"), response.Items[0].Id);
        Assert.Equal("a", response.Items[1].Code);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), response.Items[1].Id);
        Assert.Equal("b", response.Items[2].Code);
    }

    private static TestAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestAppDbContext(options);
    }

    private static Tag CreateTag(Guid id, string code, string displayName, DateTimeOffset createdAt)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;

        typeof(Tag).GetProperty(nameof(Tag.Id))!.SetValue(tag, id);
        typeof(Tag).GetProperty(nameof(Tag.Code))!.SetValue(tag, code);
        typeof(Tag).GetProperty(nameof(Tag.DisplayName))!.SetValue(tag, displayName);
        typeof(Tag).GetProperty(nameof(Tag.CreatedAt))!.SetValue(tag, createdAt);

        return tag;
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
