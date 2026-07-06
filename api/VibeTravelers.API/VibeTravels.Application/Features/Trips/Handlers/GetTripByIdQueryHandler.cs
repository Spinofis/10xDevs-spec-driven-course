using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Tags.Queries.Models;
using VibeTravels.Application.Features.Trips.Queries;
using VibeTravels.Application.Features.Trips.Queries.Models;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Features.Trips.Handlers;

public sealed class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, Result<GetTripByIdQueryResponse>>
{
    private readonly IAppDbContext _db;

    public GetTripByIdQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GetTripByIdQueryResponse>> Handle(
        GetTripByIdQuery request,
        CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .AsNoTracking()
            .Include(t => t.TripTags)
            .ThenInclude(tt => tt.Tag)
            .SingleOrDefaultAsync(
                t => t.Id == request.Request.TripId
                     && t.UserId == request.UserId
                     && t.DeletedAt == null,
                cancellationToken);

        if (trip is null)
            return Result<GetTripByIdQueryResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var tags = trip.TripTags
            .OrderBy(t => t.Order ?? 0)
            .ThenBy(t => t.TagId)
            .Select(t => new TripTagQueryModel(
                MapTag(t.Tag),
                t.Order,
                t.CreatedAt))
            .ToArray();

        return Result<GetTripByIdQueryResponse>.Ok(new GetTripByIdQueryResponse(MapTrip(trip), tags));
    }

    private static TripQueryModel MapTrip(Trip trip)
    {
        return new TripQueryModel(
            trip.Id,
            trip.UserId,
            trip.Title.Value,
            trip.PlaceText?.Value,
            trip.NoteText,
            trip.DateFrom,
            trip.DateTo,
            trip.StayLengthMinDays,
            trip.StayLengthMaxDays,
            trip.PeopleCount,
            EnumParsing.ParseNullable<BudgetLevel>(trip.BudgetLevel),
            EnumParsing.ParseNullable<Pace>(trip.Pace),
            trip.GeneratedAt,
            trip.HasGeneratedPlan,
            trip.CreatedAt,
            trip.UpdatedAt);
    }

    private static TagQueryModel MapTag(Tag tag)
    {
        return new TagQueryModel(tag.Id, tag.Code, tag.DisplayName, tag.CreatedAt);
    }

}
