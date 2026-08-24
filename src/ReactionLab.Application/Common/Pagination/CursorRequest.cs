namespace ReactionLab.Application.Common.Pagination;

public record CursorRequest
{
    private const int MaxPageSize = 100;

    private const int DefaultPageSize = 20;

    public string? Cursor { get; init; }

    public int? PageSize { get; init; }

    public int Limit => PageSize is null or < 1
        ? DefaultPageSize
        : PageSize > MaxPageSize ? MaxPageSize : PageSize.Value;
}
