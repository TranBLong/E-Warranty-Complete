using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

/// <summary>
/// Unified query parameters for list endpoints (Buổi 11 – Pagination, Filtering, Searching, Sorting).
/// SortBy is validated against a whitelist per controller.
/// </summary>
public class QueryParameters
{
    private const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;

    /// <summary>Free-text keyword for search.</summary>
    public string? Keyword { get; set; }

    /// <summary>Property name to sort by (must be in controller whitelist).</summary>
    public string? SortBy { get; set; }

    /// <summary>When true, sort descending.</summary>
    public bool IsDescending { get; set; }

    public int Skip => Math.Max(0, (Math.Max(1, PageNumber) - 1) * Take);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);

    public string? NormalizedKeyword =>
        string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim().ToLowerInvariant();
}

/// <summary>Generic paged result.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, pageNumber, pageSize, totalCount, totalPages);
    }
}
