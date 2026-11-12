/// <summary>
/// Loads one page of items for a table in server mode. The table cancels the token as soon as a newer request starts.
/// </summary>
public delegate ValueTask<FancyItemsResult<TItem>> FancyItemsProvider<TItem>(FancyItemsRequest request, CancellationToken cancellationToken);

/// <summary>
/// Describes the page a table in server mode needs: search text, sort, column filters and the 1-based page.
/// </summary>
public sealed record FancyItemsRequest(
    string SearchText,
    string? SortColumnKey,
    bool SortAscending,
    IReadOnlyList<FancyColumnFilter> ColumnFilters,
    int Page,
    int PageSize);

/// <summary>
/// One page of items returned by a <see cref="FancyItemsProvider{TItem}"/> and whether a next page exists.
/// </summary>
public sealed record FancyItemsResult<TItem>(IReadOnlyList<TItem> Items, bool HasMore)
{
    /// <summary>
    /// Number of items matching the request over all pages, or <c>null</c> if the data source does not count them.
    /// When set, the pager shows the page numbers and the summary the total like in client mode, and the next page
    /// follows from the total instead of <see cref="HasMore"/>. Negative values count as <c>null</c>.
    /// </summary>
    public int? TotalCount { get; init; }
}
