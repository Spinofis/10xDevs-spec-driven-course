using MediatR;
using Microsoft.EntityFrameworkCore;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Trips.Queries;
using VibeTravels.Application.Features.Trips.Queries.Models;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Features.Trips.Handlers;

public sealed class ListTripsQueryHandler : IRequestHandler<ListTripsQuery, Result<ListTripsQueryResponse>>
{
    private readonly IAppDbContext _db;

    public ListTripsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ListTripsQueryResponse>> Handle(ListTripsQuery request, CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (userExists is false)
            return Result<ListTripsQueryResponse>.Fail(ResultErrors.Validation("User does not exist.", nameof(request.UserId)));

        var sort = ListTripsCursor.ParseSortOrDefault(request.Request.Sort);
        var limit = request.Request.Limit ?? 20;

        var queryable = _db.Trips
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId);

        if (request.Request.IncludeDeleted is not true)
            queryable = queryable.Where(t => t.DeletedAt == null);

        if (string.IsNullOrWhiteSpace(request.Request.Query) is false)
        {
            var queryText = request.Request.Query.Trim();
            var queryPattern = $"%{queryText}%";

            queryable = queryable.Where(t =>
                EF.Functions.ILike(EF.Property<string>(t, "_title"), queryPattern)
                || EF.Functions.ILike(EF.Property<string?>(t, "_placeText") ?? string.Empty, queryPattern)
                || EF.Functions.ILike(t.NoteText ?? string.Empty, queryPattern));
        }

        if (request.Request.HasPlan is not null)
            queryable = queryable.Where(t => t.HasGeneratedPlan == request.Request.HasPlan.Value);

        if (ListTripsCursor.TryDecode(request.Request.Cursor, out var cursor))
        {
            queryable = ApplyCursor(queryable, sort, cursor);
        }

        queryable = ApplyOrdering(queryable, sort);

        var rows = await queryable
            .Take(limit + 1)
            .Select(t => new
            {
                t.Id,
                t.UserId,
                Title = EF.Property<string>(t, "_title"),
                PlaceText = EF.Property<string?>(t, "_placeText"),
                t.NoteText,
                t.DateFrom,
                t.DateTo,
                t.StayLengthMinDays,
                t.StayLengthMaxDays,
                t.PeopleCount,
                t.BudgetLevel,
                t.Pace,
                t.GeneratedAt,
                t.HasGeneratedPlan,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var hasNext = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        string? nextCursor = null;
        if (hasNext && page.Count > 0)
        {
            var last = page[^1];
            nextCursor = sort.Field switch
            {
                ListTripsCursor.SortField.CreatedAt => ListTripsCursor.Encode(
                    sort,
                    last.CreatedAt.ToString("O"),
                    last.Id,
                    lastIsNull: null),
                ListTripsCursor.SortField.GeneratedAt => ListTripsCursor.Encode(
                    sort,
                    last.GeneratedAt?.ToString("O"),
                    last.Id,
                    lastIsNull: last.GeneratedAt is null),
                ListTripsCursor.SortField.Title => ListTripsCursor.Encode(
                    sort,
                    last.Title,
                    last.Id,
                    lastIsNull: null),
                _ => null
            };
        }

        var items = page
            .Select(t => new TripQueryModel(
                t.Id,
                t.UserId,
                t.Title,
                t.PlaceText,
                t.NoteText,
                t.DateFrom,
                t.DateTo,
                t.StayLengthMinDays,
                t.StayLengthMaxDays,
                t.PeopleCount,
                EnumParsing.ParseNullable<BudgetLevel>(t.BudgetLevel),
                EnumParsing.ParseNullable<Pace>(t.Pace),
                t.GeneratedAt,
                t.HasGeneratedPlan,
                t.CreatedAt,
                t.UpdatedAt))
            .ToArray();

        var response = new ListTripsQueryResponse(items, nextCursor);
        return Result<ListTripsQueryResponse>.Ok(response);
    }

    private static IQueryable<Domain.Entities.Trips.Trip> ApplyOrdering(
        IQueryable<Domain.Entities.Trips.Trip> queryable,
        ListTripsCursor.SortSpec sort)
    {
        return sort.Field switch
        {
            ListTripsCursor.SortField.CreatedAt => sort.Desc
                ? queryable.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
                : queryable.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),

            ListTripsCursor.SortField.GeneratedAt => sort.Desc
                ? queryable
                    .OrderBy(t => t.GeneratedAt == null)
                    .ThenByDescending(t => t.GeneratedAt)
                    .ThenByDescending(t => t.Id)
                : queryable
                    .OrderBy(t => t.GeneratedAt == null)
                    .ThenBy(t => t.GeneratedAt)
                    .ThenBy(t => t.Id),

            ListTripsCursor.SortField.Title => sort.Desc
                ? queryable.OrderByDescending(t => EF.Property<string>(t, "_title")).ThenByDescending(t => t.Id)
                : queryable.OrderBy(t => EF.Property<string>(t, "_title")).ThenBy(t => t.Id),

            _ => queryable
        };
    }

    private static IQueryable<Domain.Entities.Trips.Trip> ApplyCursor(
        IQueryable<Domain.Entities.Trips.Trip> queryable,
        ListTripsCursor.SortSpec sort,
        ListTripsCursor.Payload cursor)
    {
        return sort.Field switch
        {
            ListTripsCursor.SortField.CreatedAt => ApplyCreatedAtCursor(
                queryable,
                sort.Desc,
                cursor.LastId,
                DateTimeOffset.Parse(cursor.LastValue!)),

            ListTripsCursor.SortField.GeneratedAt => ApplyGeneratedAtCursor(
                queryable,
                sort.Desc,
                cursor),

            ListTripsCursor.SortField.Title => ApplyTitleCursor(
                queryable,
                sort.Desc,
                cursor.LastId,
                cursor.LastValue!),

            _ => queryable
        };
    }

    private static IQueryable<Domain.Entities.Trips.Trip> ApplyCreatedAtCursor(
        IQueryable<Domain.Entities.Trips.Trip> queryable,
        bool desc,
        Guid lastId,
        DateTimeOffset lastValue)
    {
        if (desc)
        {
            return queryable.Where(t =>
                t.CreatedAt < lastValue
                || (t.CreatedAt == lastValue && t.Id.CompareTo(lastId) < 0));
        }

        return queryable.Where(t =>
            t.CreatedAt > lastValue
            || (t.CreatedAt == lastValue && t.Id.CompareTo(lastId) > 0));
    }

    private static IQueryable<Domain.Entities.Trips.Trip> ApplyGeneratedAtCursor(
        IQueryable<Domain.Entities.Trips.Trip> queryable,
        bool desc,
        ListTripsCursor.Payload cursor)
    {
        var lastIsNull = cursor.LastIsNull ?? (cursor.LastValue is null);

        if (lastIsNull)
        {
            return desc
                ? queryable.Where(t => t.GeneratedAt == null && t.Id.CompareTo(cursor.LastId) < 0)
                : queryable.Where(t => t.GeneratedAt == null && t.Id.CompareTo(cursor.LastId) > 0);
        }

        var lastValue = DateTimeOffset.Parse(cursor.LastValue!);

        if (desc)
        {
            return queryable.Where(t =>
                (t.GeneratedAt == null)
                || (t.GeneratedAt != null && (t.GeneratedAt < lastValue || (t.GeneratedAt == lastValue && t.Id.CompareTo(cursor.LastId) < 0))));
        }

        return queryable.Where(t =>
            (t.GeneratedAt == null)
            || (t.GeneratedAt != null && (t.GeneratedAt > lastValue || (t.GeneratedAt == lastValue && t.Id.CompareTo(cursor.LastId) > 0))));
    }

    private static IQueryable<Domain.Entities.Trips.Trip> ApplyTitleCursor(
        IQueryable<Domain.Entities.Trips.Trip> queryable,
        bool desc,
        Guid lastId,
        string lastTitle)
    {
        if (desc)
        {
            return queryable.Where(t =>
                string.Compare(EF.Property<string>(t, "_title"), lastTitle) < 0
                || (EF.Property<string>(t, "_title") == lastTitle && t.Id.CompareTo(lastId) < 0));
        }

        return queryable.Where(t =>
            string.Compare(EF.Property<string>(t, "_title"), lastTitle) > 0
            || (EF.Property<string>(t, "_title") == lastTitle && t.Id.CompareTo(lastId) > 0));
    }

}
