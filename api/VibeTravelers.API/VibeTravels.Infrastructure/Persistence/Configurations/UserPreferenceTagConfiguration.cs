using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class UserPreferenceTagConfiguration : IEntityTypeConfiguration<UserPreferenceTag>
{
    public void Configure(EntityTypeBuilder<UserPreferenceTag> builder)
    {
        builder.ToTable("user_preference_tags");

        builder.HasKey(pt => new { pt.UserId, pt.TagId });

        builder.Property(pt => pt.UserId)
            .HasColumnName("user_id");

        builder.Property(pt => pt.TagId)
            .HasColumnName("tag_id");

        builder.Property(pt => pt.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(pt => pt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(pt => pt.Tag)
            .WithMany()
            .HasForeignKey(pt => pt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
