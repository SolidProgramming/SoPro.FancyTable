using Microsoft.AspNetCore.Components;

public sealed class FancyRowContext<TItem>
{
    public required TItem Item { get; init; }
    public required IReadOnlyList<FancyColumn<TItem>> Columns { get; init; }
    public required EventCallback ToggleNode { get; init; }
    public required string ExpandLabel { get; init; }
    public required string CollapseLabel { get; init; }

    public bool IsTreeNode { get; init; }
    public int Depth { get; init; }
    public bool HasChildren { get; init; }
    public bool IsExpanded { get; init; }

    public int VisibleColumnCount => Columns.Count;
    public bool CanToggle => IsTreeNode && HasChildren;
}
