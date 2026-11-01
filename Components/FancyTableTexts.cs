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
    public string ResizeColumnFormat { get; init; } = "Resize column {0}";
    public string ReorderColumnFormat { get; init; } = "Reorder column {0}";
    public string SortColumnFormat { get; init; } = "Sort column {0}";
    public string HideColumnFormat { get; init; } = "Hide column {0}";
    public string FilteredColumnLabel { get; init; } = "filtered";
    public string FilterOperatorLabel { get; init; } = "Operator";
    public string ClearSearchLabel { get; init; } = "Clear search";
    public string FilterColumnFormat { get; init; } = "Filter column {0}";
    public string FilterValuePlaceholder { get; init; } = "Value";
    public string ApplyFilterLabel { get; init; } = "Apply";
    public string ClearFilterLabel { get; init; } = "Clear";
    public string FilterOperatorEqualsLabel { get; init; } = "Is";
    public string FilterOperatorNotEqualsLabel { get; init; } = "Is not";
    public string FilterOperatorContainsLabel { get; init; } = "Contains";
    public string FilterOperatorNotContainsLabel { get; init; } = "Does not contain";
    public string FilterOperatorStartsWithLabel { get; init; } = "Starts with";
    public string FilterOperatorEndsWithLabel { get; init; } = "Ends with";
    public string FilterOperatorIsEmptyLabel { get; init; } = "Is empty";
    public string FilterOperatorIsNotEmptyLabel { get; init; } = "Is not empty";
}
