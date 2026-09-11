namespace LeanPortal.Application.Common;

/// <summary>A page of results plus the metadata a client needs to render a pager.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
        => new() { Items = [], Page = page, PageSize = pageSize, TotalCount = 0 };
}

/// <summary>Common query-string parameters for list endpoints.</summary>
public class PagedQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => value };
    }

    /// <summary>Free-text search term applied to the natural title/name field of the resource.</summary>
    public string? Search { get; set; }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>Uniform error payload returned by the API for handled failures.</summary>
public class ApiError
{
    public string Message { get; init; } = string.Empty;
    public string? Code { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public string? TraceId { get; init; }
}
