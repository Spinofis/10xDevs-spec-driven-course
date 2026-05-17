using System.Security.Cryptography;
using System.Text;
using VibeTravels.Application.Features.Jobs.Models;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Features.Trips.Services;

public sealed class TripInputFingerprintService : ITripInputFingerprintService
{
    public Result<TripInputFingerprint> Build(Trip trip, Guid userId)
    {
        var orderedTags = trip.TripTags
            .OrderBy(x => x.Order ?? 0)
            .ThenBy(x => x.TagId)
            .Select(x => new GenerationJobRequestPayloadTag(
                x.TagId,
                x.Tag.Code,
                x.Tag.DisplayName,
                x.Order ?? 0))
            .ToArray();

        var payload = new GenerationJobRequestPayload(
            trip.Id,
            userId,
            trip.Title.Value,
            trip.PlaceText?.Value,
            trip.NoteText,
            trip.DateFrom,
            trip.DateTo,
            trip.StayLengthMinDays,
            trip.StayLengthMaxDays,
            trip.PeopleCount,
            trip.BudgetLevel,
            trip.Pace,
            orderedTags);

        try
        {
            var payloadJson = payload.ToJson();
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return Result<TripInputFingerprint>.Ok(new TripInputFingerprint(payloadJson, hash));
        }
        catch (Exception exception)
        {
            return Result<TripInputFingerprint>.Fail(
                ResultErrors.Unknown($"Failed to serialize generation payload: {exception.Message}"));
        }
    }
}
