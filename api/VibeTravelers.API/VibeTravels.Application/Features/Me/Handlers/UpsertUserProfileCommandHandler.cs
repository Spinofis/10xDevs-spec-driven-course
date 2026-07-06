using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Me.Commands;
using VibeTravels.Domain.Common.Results;
using VibeTravels.Domain.Entities.Users;

namespace VibeTravels.Application.Features.Me.Handlers;

public sealed class UpsertUserProfileCommandHandler : IRequestHandler<UpsertUserProfileCommand, Result>
{
    private readonly IAppDbContext _db;

    public UpsertUserProfileCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(
        UpsertUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var requestedTags = request.Request.PreferenceTags ?? [];
        var requestedTagIds = requestedTags
            .Select(t => t.TagId)
            .Distinct()
            .ToArray();

        var tagsById = requestedTagIds.Length == 0
            ? new Dictionary<Guid, Domain.Entities.Tags.Tag>()
            : await _db.Tags
                .Where(t => requestedTagIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        if (tagsById.Count != requestedTagIds.Length)
            return Result.Fail(ResultErrors.TagNotFound(nameof(request.Request.PreferenceTags)));

        var profile = await _db.UserProfiles
            .SingleOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        var profileModel = request.Request.Profile;

        if (profile is null)
        {
            profile = UserProfile.Create(request.UserId);
            profile.Update(
                profileModel.DefaultBudgetLevel?.ToString(),
                profileModel.DefaultPeopleCount,
                profileModel.DefaultPace?.ToString(),
                profileModel.DefaultNotes,
                profileModel.IsDefault);
            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.Update(
                profileModel.DefaultBudgetLevel?.ToString(),
                profileModel.DefaultPeopleCount,
                profileModel.DefaultPace?.ToString(),
                profileModel.DefaultNotes,
                profileModel.IsDefault);
        }

        var existingTags = await _db.UserPreferenceTags
            .Where(pt => pt.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingTags)
            _db.UserPreferenceTags.Remove(existing);

        var newTags = requestedTags
            .Select(t => UserPreferenceTag.Create(request.UserId, t.TagId, t.Order))
            .ToList();

        if (newTags.Count > 0)
            _db.UserPreferenceTags.AddRange(newTags);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
