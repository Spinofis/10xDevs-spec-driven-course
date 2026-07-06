using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Application.Features.Trips.Queries.Models;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Features.Trips.Handlers;

public sealed class PatchTripCommandHandler : IRequestHandler<PatchTripCommand, Result<PatchTripCommandResponse>>
{
    private readonly IAppDbContext _db;

    public PatchTripCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PatchTripCommandResponse>> Handle(PatchTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .Include(t => t.TripTags)
            .SingleOrDefaultAsync(
                t => t.Id == request.TripId && t.UserId == request.UserId && t.DeletedAt == null,
                cancellationToken);

        if (trip is null)
            return Result<PatchTripCommandResponse>.Fail(ResultErrors.TripNotFound(nameof(request.TripId)));

        var model = request.Request.ToModel();

        var tagSyncResult = await SyncTripTagsAsync(trip, model, cancellationToken);
        if (tagSyncResult.IsSuccess is false)
            return Result<PatchTripCommandResponse>.Fail(tagSyncResult.Errors);

        var hasAnyTags = model.Tags.IsSet
            ? model.Tags.Value.Count > 0
            : trip.TripTags.Count > 0;

        var patchResult = trip.ApplyPatch(
            title: model.Title.IsSet ? model.Title.Value : trip.Title.Value,
            placeText: model.PlaceText.IsSet ? model.PlaceText.Value : trip.PlaceText?.Value,
            noteText: model.NoteText.IsSet ? model.NoteText.Value : trip.NoteText,
            dateFrom: model.DateFrom.IsSet ? model.DateFrom.Value : trip.DateFrom,
            dateTo: model.DateTo.IsSet ? model.DateTo.Value : trip.DateTo,
            stayLengthMinDays: model.StayLengthMinDays.IsSet ? model.StayLengthMinDays.Value : trip.StayLengthMinDays,
            stayLengthMaxDays: model.StayLengthMaxDays.IsSet ? model.StayLengthMaxDays.Value : trip.StayLengthMaxDays,
            peopleCount: model.PeopleCount.IsSet ? model.PeopleCount.Value : trip.PeopleCount,
            budgetLevel: model.BudgetLevel.IsSet
                ? model.BudgetLevel.Value?.ToString()
                : trip.BudgetLevel,
            pace: model.Pace.IsSet
                ? model.Pace.Value?.ToString()
                : trip.Pace,
            hasAnyTags: hasAnyTags);

        if (patchResult.IsSuccess is false)
            return Result<PatchTripCommandResponse>.Fail(patchResult.Errors);

        if (tagSyncResult.Value)
            trip.TouchUpdatedAt();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<PatchTripCommandResponse>.Ok(new PatchTripCommandResponse(MapTrip(trip)));
    }

    private async Task<Result<bool>> SyncTripTagsAsync(
        Trip trip,
        PatchTripCommandModel model,
        CancellationToken cancellationToken)
    {
        if (model.Tags.IsSet is false)
            return Result<bool>.Ok(false);

        var requestedTags = model.Tags.Value!;
        var requestedTagIds = requestedTags
            .Select(x => x.TagId)
            .Distinct()
            .ToArray();

        var existingTagIds = requestedTagIds.Length == 0
            ? Array.Empty<Guid>()
            : await _db.Tags
                .Where(t => requestedTagIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToArrayAsync(cancellationToken);

        if (existingTagIds.Length != requestedTagIds.Length)
            return Result<bool>.Fail(ResultErrors.Validation("One or more tags were not found.", nameof(model.Tags)));

        var changed = false;
        var currentByTagId = trip.TripTags.ToDictionary(x => x.TagId);
        var requestedByTagId = requestedTags.ToDictionary(x => x.TagId);

        var toRemove = trip.TripTags
            .Where(x => requestedByTagId.ContainsKey(x.TagId) is false)
            .ToArray();

        foreach (var tripTag in toRemove)
        {
            trip.TripTags.Remove(tripTag);
            _db.TripTags.Remove(tripTag);
            changed = true;
        }

        foreach (var requested in requestedTags)
        {
            if (currentByTagId.TryGetValue(requested.TagId, out var existing))
            {
                if (existing.UpdateOrder(requested.Order))
                    changed = true;

                continue;
            }

            var newTripTagResult = TripTag.Create(trip.Id, requested.TagId, requested.Order);
            if (newTripTagResult.IsSuccess is false || newTripTagResult.Value is null)
                return Result<bool>.Fail(newTripTagResult.Errors);

            trip.TripTags.Add(newTripTagResult.Value);
            changed = true;
        }

        return Result<bool>.Ok(changed);
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

}
