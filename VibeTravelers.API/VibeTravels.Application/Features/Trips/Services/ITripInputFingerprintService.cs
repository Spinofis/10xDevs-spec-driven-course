using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Features.Trips.Services;

public interface ITripInputFingerprintService
{
    Result<TripInputFingerprint> Build(Trip trip, Guid userId);
}
