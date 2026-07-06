using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Me.Queries;
using VibeTravels.Application.Features.Me.Queries.Models;
using VibeTravels.Application.Features.Tags.Queries.Models;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Me.Handlers;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<GetUserProfileQueryResponse>>
{
    private readonly IAppDbContext _db;

    public GetUserProfileQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GetUserProfileQueryResponse>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        var preferenceTags = await _db.UserPreferenceTags
            .AsNoTracking()
            .Include(pt => pt.Tag)
            .Where(pt => pt.UserId == request.UserId)
            .OrderBy(pt => pt.Order)
            .ThenBy(pt => pt.TagId)
            .ToListAsync(cancellationToken);

        UserProfileQueryModel profileModel;
        if (profile is null)
        {
            var now = DateTimeOffset.UtcNow;
            profileModel = new UserProfileQueryModel(
                DefaultBudgetLevel: null,
                DefaultPeopleCount: null,
                DefaultPace: null,
                DefaultNotes: null,
                IsDefault: true,
                CreatedAt: now,
                UpdatedAt: now);
        }
        else
        {
            profileModel = new UserProfileQueryModel(
                DefaultBudgetLevel: ParseEnum<BudgetLevel>(profile.DefaultBudgetLevel),
                DefaultPeopleCount: profile.DefaultPeopleCount,
                DefaultPace: ParseEnum<Pace>(profile.DefaultPace),
                DefaultNotes: profile.DefaultNotes,
                IsDefault: profile.IsDefault,
                CreatedAt: new DateTimeOffset(profile.CreatedAt, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(profile.UpdatedAt, TimeSpan.Zero));
        }

        var tagModels = preferenceTags
            .Select(pt => new PreferenceTagQueryModel(
                Tag: new TagQueryModel(pt.Tag.Id, pt.Tag.Code, pt.Tag.DisplayName, pt.Tag.CreatedAt),
                Order: pt.Order,
                CreatedAt: new DateTimeOffset(pt.CreatedAt, TimeSpan.Zero)))
            .ToList();

        return Result<GetUserProfileQueryResponse>.Ok(
            new GetUserProfileQueryResponse(request.UserId, profileModel, tagModels));
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
