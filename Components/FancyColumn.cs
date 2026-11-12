using Microsoft.AspNetCore.Components;

public sealed class FancyColumn<TItem>
{
    public required string Key { get; init; }
    public required string Title { get; init; }

    public bool Sortable { get; init; }
    public bool Searchable { get; init; } = true;
    public bool Hideable { get; init; }
    public bool Resizable { get; init; } = true;
    public bool Reorderable { get; init; } = true;
    public bool Filterable { get; init; }

    // Operators offered in the filter popup, in the given order; null or empty offers all operators.
    public IReadOnlyList<FancyColumnFilterOperator>? FilterOperators { get; init; }

    // Kind of values the filter compares: Text (default), Date or Number; selects the filter input and operators.
    public FancyColumnFilterType FilterType { get; init; } = FancyColumnFilterType.Text;

    public double? Width { get; init; }
    public double? MinWidth { get; init; }
    public double? MaxWidth { get; init; }

    public string? HeaderClass { get; init; }
    public string? CellClass { get; init; }

    // If no CellTemplate is provided, this is used for display.
    public Func<TItem, object?>? ValueSelector { get; init; }

    // Optional custom sort value (falls back to ValueSelector).
    public Func<TItem, IComparable?>? SortValueSelector { get; init; }

    // Optional custom search text (falls back to ValueSelector?.ToString()).
    public Func<TItem, string?>? SearchTextSelector { get; init; }

    public RenderFragment<TItem>? CellTemplate { get; init; }

    // Inline min-width / max-width of a column without a fixed width; null if neither is set, so the browser sizes it (auto).
    internal string? GetWidthBoundsStyle()
    {
        string minWidthStyle = MinWidth is double minWidth ? FormattableString.Invariant($"min-width:{minWidth:0.##}px;") : "";
        string maxWidthStyle = MaxWidth is double maxWidth ? FormattableString.Invariant($"max-width:{maxWidth:0.##}px;") : "";
        string style = minWidthStyle + maxWidthStyle;
        return style.Length == 0 ? null : style;
    }
}
