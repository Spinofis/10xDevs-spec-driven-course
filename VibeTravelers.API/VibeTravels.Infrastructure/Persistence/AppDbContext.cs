using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
