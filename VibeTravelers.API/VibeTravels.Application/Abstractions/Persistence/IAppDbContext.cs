using Microsoft.EntityFrameworkCore;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
