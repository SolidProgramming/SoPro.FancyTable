using Microsoft.AspNetCore.Components;

public sealed class FancyColumn<TItem>
{
    public required string Key { get; init; }
    public required string Title { get; init; }

    public bool Sortable { get; init; }
    public bool Searchable { get; init; } = true;
    public bool Hideable { get; init; }

    public string? HeaderClass { get; init; }
    public string? CellClass { get; init; }

    // If no CellTemplate is provided, this is used for display.
    public Func<TItem, object?>? ValueSelector { get; init; }

    // Optional custom sort value (falls back to ValueSelector).
    public Func<TItem, IComparable?>? SortValueSelector { get; init; }

    // Optional custom search text (falls back to ValueSelector?.ToString()).
    public Func<TItem, string?>? SearchTextSelector { get; init; }

    public RenderFragment<TItem>? CellTemplate { get; init; }
}