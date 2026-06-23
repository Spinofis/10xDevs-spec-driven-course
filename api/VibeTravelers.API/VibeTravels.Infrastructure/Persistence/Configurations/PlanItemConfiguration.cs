using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Infrastructure.Persistence.Configurations;

public sealed class PlanItemConfiguration : IEntityTypeConfiguration<PlanItem>
{
    public void Configure(EntityTypeBuilder<PlanItem> builder)
    {
        builder.ToTable("plan_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(x => x.DayNumber)
            .HasColumnName("day_number")
            .IsRequired();

        builder.Property(x => x.ItemDate)
            .HasColumnName("item_date")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(x => x.PlaceType)
            .HasColumnName("place_type")
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => ParsePlaceType(value))
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.LocationText)
            .HasColumnName("location_text");

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

        builder.HasIndex(x => new { x.TripId, x.DayNumber, x.SortOrder, x.Id })
            .HasDatabaseName("plan_items_trip_id_day_order_idx");
    }

    private static PlanItemPlaceType ParsePlaceType(string value)
    {
        return value switch
        {
            "attraction" => PlanItemPlaceType.Attraction,
            "restaurant" => PlanItemPlaceType.Restaurant,
            "hotel" => PlanItemPlaceType.Hotel,
            _ => throw new InvalidOperationException($"Unknown plan item place type '{value}'.")
        };
    }
}
