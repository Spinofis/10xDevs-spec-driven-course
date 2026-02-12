using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Tags.Queries;
using VibeTravels.Application.Features.Tags.Queries.Models;

namespace VibeTravels.Application.Features.Tags.Handlers;

public sealed class ListTagsQueryHandler : IRequestHandler<ListTagsQuery, ListTagsQueryResponse>
{
    private readonly IAppDbContext _db;

    public ListTagsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ListTagsQueryResponse> Handle(ListTagsQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ThenBy(t => t.Id)
            .Select(t => new TagQueryModel(
                t.Id,
                t.Code,
                t.DisplayName,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ListTagsQueryResponse(items);
    }
}
