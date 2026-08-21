public sealed record FancyTableTexts
{
    public static FancyTableTexts Default { get; } = new();

    public string SearchPlaceholder { get; init; } = "Search";
    public string RowsPerPageLabel { get; init; } = "Rows per page";
    public string AllItemsLabel { get; init; } = "All";
    public string HiddenColumnsLabel { get; init; } = "Hidden columns:";
    public string PaginationAriaLabel { get; init; } = "Pagination";
    public string PreviousPageLabel { get; init; } = "Previous";
    public string NextPageLabel { get; init; } = "Next";
    public string NoRowsLabel { get; init; } = "No rows";
    public string NoMatchingRowsFormat { get; init; } = "No matching rows";
    public string NoMatchingRowsWithTotalFormat { get; init; } = "No matching rows ({0} total)";
    public string ShowingItemsFormat { get; init; } = "Showing {0}-{1} of {2}";
    public string ShowingFilteredItemsFormat { get; init; } = "Showing {0}-{1} of {2} filtered rows ({3} total)";
    public string ExpandLabel { get; init; } = "Expand";
    public string CollapseLabel { get; init; } = "Collapse";
    public string NoMatchingRootItemsFormat { get; init; } = "No matching rows ({0} total root items)";
    public string ShowingVisibleRowsFormat { get; init; } = "Showing {0}-{1} of {2} visible rows";
}
