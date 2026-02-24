using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Tags;

namespace VibeTravels.Domain.Entities.Trips;

public sealed class TripTag
{
    public Guid TripId { get; private set; }
    public Guid TagId { get; private set; }
    public int? Order { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Trip Trip { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private TripTag()
    {
    }

    public static Result<TripTag> Create(Guid tripId, Guid tagId, int? order = 5)
    {
        if (tripId == Guid.Empty)
            return Result<TripTag>.Fail(ResultErrors.Validation("TripId is required.", nameof(TripId)));

        if (tagId == Guid.Empty)
            return Result<TripTag>.Fail(ResultErrors.Validation("TagId is required.", nameof(TagId)));

        return Result<TripTag>.Ok(new TripTag
        {
            TripId = tripId,
            TagId = tagId,
            Order = order ?? 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

