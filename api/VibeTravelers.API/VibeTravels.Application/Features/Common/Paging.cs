namespace VibeTravels.Application.Features.Common;

public interface IPagedRequest
{
    int? Limit { get; }
    string? Cursor { get; }
}

public interface ISortableRequest
{
    string? Sort { get; }
}

public record PagedResponse<T>(IReadOnlyList<T> Items, string? NextCursor = null);
