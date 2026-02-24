using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Tags.Queries.Models;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Application.Features.Trips.Queries.Models;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Features.Trips.Handlers;

public sealed class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Result<CreateTripCommandResponse>>
{
    private readonly IAppDbContext _db;

    public CreateTripCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CreateTripCommandResponse>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        var model = request.Request.Model;
        var requestedTags = model.Tags ?? Array.Empty<TripTagCommandModel>();

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (userExists is false)
            return Result<CreateTripCommandResponse>.Fail(ResultErrors.Validation("User does not exist.", nameof(request.UserId)));

        var tripResult = Trip.Create(
            request.UserId,
            model.Title,
            model.PlaceText,
            model.NoteText,
            model.DateFrom,
            model.DateTo,
            model.StayLengthMinDays,
            model.StayLengthMaxDays,
            model.PeopleCount,
            model.BudgetLevel?.ToString(),
            model.Pace?.ToString(),
            requestedTags.Count > 0);

        if (tripResult.IsSuccess is false || tripResult.Value is null)
            return Result<CreateTripCommandResponse>.Fail(tripResult.Errors);

        var trip = tripResult.Value;

        var requestedTagIds = requestedTags
            .Select(t => t.TagId)
            .Distinct()
            .ToArray();

        var tagsById = requestedTagIds.Length == 0
            ? new Dictionary<Guid, Tag>()
            : await _db.Tags
                .Where(t => requestedTagIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        if (tagsById.Count != requestedTagIds.Length)
            return Result<CreateTripCommandResponse>.Fail(ResultErrors.TagNotFound(nameof(model.Tags)));

        var tripTags = new List<TripTag>(requestedTags.Count);
        foreach (var item in requestedTags)
        {
            var tripTagResult = TripTag.Create(trip.Id, item.TagId, item.Order);
            if (tripTagResult.IsSuccess is false || tripTagResult.Value is null)
                return Result<CreateTripCommandResponse>.Fail(tripTagResult.Errors);

            tripTags.Add(tripTagResult.Value);
        }

        _db.Trips.Add(trip);
        if (tripTags.Count > 0)
            _db.TripTags.AddRange(tripTags);

        await _db.SaveChangesAsync(cancellationToken);

        var response = new CreateTripCommandResponse(
            MapTrip(trip),
            tripTags
                .OrderBy(x => x.Order ?? 0)
                .ThenBy(x => x.TagId)
                .Select(x => new TripTagQueryModel(
                    MapTag(tagsById[x.TagId]),
                    x.Order,
                    x.CreatedAt))
                .ToArray());

        return Result<CreateTripCommandResponse>.Ok(response);
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
            ParseEnum<BudgetLevel>(trip.BudgetLevel),
            ParseEnum<Pace>(trip.Pace),
            trip.GeneratedAt,
            trip.HasGeneratedPlan,
            trip.CreatedAt,
            trip.UpdatedAt);
    }

    private static TagQueryModel MapTag(Tag tag)
    {
        return new TagQueryModel(tag.Id, tag.Code, tag.DisplayName, tag.CreatedAt);
    }

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
