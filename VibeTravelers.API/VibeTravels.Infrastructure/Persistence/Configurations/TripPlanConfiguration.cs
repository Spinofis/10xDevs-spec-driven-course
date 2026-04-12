using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class TripPlanConfiguration : IEntityTypeConfiguration<TripPlan>
{
    public void Configure(EntityTypeBuilder<TripPlan> builder)
    {
        builder.ToTable("trip_plans");

        builder.HasKey(x => x.TripId);

        builder.Property(x => x.TripId)
            .HasColumnName("trip_id");

        builder.Property(x => x.GenerationJobId)
            .HasColumnName("generation_job_id");

        builder.Property(x => x.Title)
            .HasColumnName("title");

        builder.Property(x => x.Summary)
            .HasColumnName("summary");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.GenerationJobId)
            .HasDatabaseName("trip_plans_generation_job_id_idx");
    }
}
