namespace ReactionLab.Application.Common.Pagination;

public record CursorPagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }

    public int PageSize { get; init; }
}