using Microsoft.EntityFrameworkCore;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Trip> Trips { get; }
    DbSet<TripTag> TripTags { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
