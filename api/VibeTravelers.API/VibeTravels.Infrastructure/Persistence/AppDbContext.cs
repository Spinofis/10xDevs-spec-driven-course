using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
