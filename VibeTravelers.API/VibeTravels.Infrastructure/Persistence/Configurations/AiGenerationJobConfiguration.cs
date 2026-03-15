using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class AiGenerationJobConfiguration : IEntityTypeConfiguration<AiGenerationJob>
{
    public void Configure(EntityTypeBuilder<AiGenerationJob> builder)
    {
        builder.ToTable("generation_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => ParseStatus(value))
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at");

        builder.Property(x => x.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(x => x.CanceledAt)
            .HasColumnName("canceled_at");

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(x => x.InputSnapshot)
            .HasColumnName("input_snapshot")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.InputHash)
            .HasColumnName("input_hash")
            .IsRequired();

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TripId, x.RequestedAt })
            .HasDatabaseName("generation_jobs_trip_id_requested_at_idx")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.Status, x.RequestedAt })
            .HasDatabaseName("generation_jobs_status_requested_at_idx")
            .IsDescending(false, true);

        builder.HasIndex(x => x.TripId)
            .HasDatabaseName("generation_jobs_one_active_per_trip_ux")
            .HasFilter("status IN ('pending','running')")
            .IsUnique();
    }

    private static AiGenerationJobStatus ParseStatus(string value)
    {
        return value switch
        {
            "pending" => AiGenerationJobStatus.Pending,
            "running" => AiGenerationJobStatus.Running,
            "succeeded" => AiGenerationJobStatus.Succeeded,
            "failed" => AiGenerationJobStatus.Failed,
            "canceled" => AiGenerationJobStatus.Canceled,
            _ => throw new InvalidOperationException($"Unknown ai generation job status '{value}'.")
        };
    }
}
