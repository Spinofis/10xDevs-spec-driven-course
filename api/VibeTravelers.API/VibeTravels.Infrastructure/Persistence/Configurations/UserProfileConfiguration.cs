using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profile");

        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).HasColumnName("user_id");

        builder.Property(p => p.DefaultBudgetLevel)
            .HasColumnName("default_budget_level");

        builder.Property(p => p.DefaultPeopleCount)
            .HasColumnName("default_people_count");

        builder.Property(p => p.DefaultPace)
            .HasColumnName("default_pace");

        builder.Property(p => p.DefaultNotes)
            .HasColumnName("default_notes");

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne<Domain.Entities.Users.User>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
