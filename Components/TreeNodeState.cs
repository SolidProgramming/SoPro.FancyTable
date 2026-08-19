public sealed class TreeNodeState<TItem> where TItem : notnull
{
    public required TItem Item { get; init; }
    public required int Depth { get; init; }
    public required bool HasChildren { get; init; }
    public required List<TreeNodeState<TItem>> Children { get; init; }
    public TreeNodeState<TItem>? Parent { get; init; }
    public bool IsExpanded { get; set; }
}

