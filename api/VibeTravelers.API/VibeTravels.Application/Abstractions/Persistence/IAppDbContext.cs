using Microsoft.EntityFrameworkCore;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserPreferenceTag> UserPreferenceTags { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Trip> Trips { get; }
    DbSet<TripTag> TripTags { get; }
    DbSet<AiGenerationJob> AiGenerationJobs { get; }
    DbSet<TripPlan> TripPlans { get; }
    DbSet<PlanItem> PlanItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
