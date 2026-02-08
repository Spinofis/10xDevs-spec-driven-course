using VibeTravels.Application.Features.Legacy.Me.Models;

namespace VibeTravels.Application.Features.Legacy.Me.Queries;

public sealed record GetUserProfileQueryRequest;

public sealed record GetUserProfileQueryResponse(UserProfileDto Profile);
