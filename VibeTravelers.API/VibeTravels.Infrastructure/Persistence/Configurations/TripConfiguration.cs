using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.ValueObjects;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(TripTitle.MaxLength)
            .HasConversion(
                value => value.Value,
                value => TripTitle.From(value))
            .IsRequired();

        builder.Property(t => t.PlaceText)
            .HasColumnName("place_text")
            .HasMaxLength(TripPlaceText.MaxLength)
            .HasConversion(
                value => value == null ? null : value.Value,
                value => string.IsNullOrWhiteSpace(value) ? null : TripPlaceText.From(value));

        builder.Property(t => t.NoteText)
            .HasColumnName("notes")
            .HasMaxLength(Trip.NoteTextMaxLength);

        builder.Property(t => t.DateFrom)
            .HasColumnName("input_date_from");

        builder.Property(t => t.DateTo)
            .HasColumnName("input_date_to");

        builder.Property(t => t.StayLengthMinDays)
            .HasColumnName("input_days_min");

        builder.Property(t => t.StayLengthMaxDays)
            .HasColumnName("input_days_max");

        builder.Property(t => t.PeopleCount)
            .HasColumnName("people_count");

        builder.Property(t => t.BudgetLevel)
            .HasColumnName("budget_level")
            .HasMaxLength(16);

        builder.Property(t => t.Pace)
            .HasColumnName("pace")
            .HasMaxLength(16);

        builder.Property(t => t.GeneratedAt)
            .HasColumnName("generated_at");

        builder.Property(t => t.HasGeneratedPlan)
            .HasColumnName("has_generated_plan")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasMany(t => t.TripTags)
            .WithOne(tt => tt.Trip)
            .HasForeignKey(tt => tt.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
