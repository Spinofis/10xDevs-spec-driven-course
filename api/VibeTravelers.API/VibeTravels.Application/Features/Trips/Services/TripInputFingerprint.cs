namespace VibeTravels.Application.Features.Trips.Services;

public sealed record TripInputFingerprint(
    string PayloadJson,
    string Hash);
