using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class TripTagConfiguration : IEntityTypeConfiguration<TripTag>
{
    public void Configure(EntityTypeBuilder<TripTag> builder)
    {
        builder.ToTable("trip_tags");

        builder.HasKey(tt => new { tt.TripId, tt.TagId });

        builder.Property(tt => tt.TripId)
            .HasColumnName("trip_id");

        builder.Property(tt => tt.TagId)
            .HasColumnName("tag_id");

        builder.Property(tt => tt.Order)
            .HasColumnName("order");

        builder.Property(tt => tt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(tt => tt.Tag)
            .WithMany()
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

