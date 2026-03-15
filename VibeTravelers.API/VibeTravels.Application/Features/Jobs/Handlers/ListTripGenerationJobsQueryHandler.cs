using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Jobs.Queries;
using VibeTravels.Application.Features.Jobs.Queries.Models;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Jobs.Handlers;

public sealed class ListTripGenerationJobsQueryHandler
    : IRequestHandler<ListTripGenerationJobsQuery, Result<ListTripGenerationJobsQueryResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IGenerationJobStatusMapper _generationJobStatusMapper;

    public ListTripGenerationJobsQueryHandler(
        IAppDbContext db,
        IGenerationJobStatusMapper generationJobStatusMapper)
    {
        _db = db;
        _generationJobStatusMapper = generationJobStatusMapper;
    }

    public async Task<Result<ListTripGenerationJobsQueryResponse>> Handle(
        ListTripGenerationJobsQuery request,
        CancellationToken cancellationToken)
    {
        var tripExists = await _db.Trips
            .AsNoTracking()
            .AnyAsync(
                t => t.Id == request.Request.TripId
                     && t.UserId == request.UserId
                     && t.DeletedAt == null,
                cancellationToken);

        if (tripExists is false)
            return Result<ListTripGenerationJobsQueryResponse>.Fail(ResultErrors.TripNotFound(nameof(request.Request.TripId)));

        var limit = request.Request.Limit ?? 20;
        var queryable = _db.AiGenerationJobs
            .AsNoTracking()
            .Where(x => x.TripId == request.Request.TripId && x.UserId == request.UserId);

        if (ListTripGenerationJobsCursor.TryDecode(request.Request.Cursor, out var cursor))
        {
            var lastRequestedAt = DateTimeOffset.Parse(cursor.LastRequestedAt);
            var lastId = cursor.LastId;

            queryable = queryable.Where(x =>
                x.RequestedAt < lastRequestedAt
                || (x.RequestedAt == lastRequestedAt && x.Id.CompareTo(lastId) < 0));
        }

        var rows = await queryable
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit + 1)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.RequestedAt,
                x.FinishedAt,
                x.Discarded,
                x.DiscardReason
            })
            .ToListAsync(cancellationToken);

        var hasNext = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        string? nextCursor = null;
        if (hasNext && page.Count > 0)
        {
            var last = page[^1];
            nextCursor = ListTripGenerationJobsCursor.Encode(last.RequestedAt, last.Id);
        }

        var items = page
            .Select(x => new GenerationJobListItemQueryModel(
                x.Id,
                _generationJobStatusMapper.Map(x.Status),
                x.RequestedAt,
                x.FinishedAt,
                x.Discarded,
                x.DiscardReason))
            .ToArray();

        return Result<ListTripGenerationJobsQueryResponse>.Ok(
            new ListTripGenerationJobsQueryResponse(items, nextCursor));
    }
}
