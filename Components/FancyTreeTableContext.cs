public sealed class FancyTreeTableContext<TItem>
{
    private readonly Func<Task> expandAllAsync;
    private readonly Func<Task> collapseAllAsync;

    public FancyTreeTableContext(
        int rootItemCount,
        int visibleNodeCount,
        int expandedNodeCount,
        bool hasExpandableNodes,
        bool canExpandAny,
        bool canCollapseAny,
        Func<Task> expandAllAsync,
        Func<Task> collapseAllAsync)
    {
        RootItemCount = rootItemCount;
        VisibleNodeCount = visibleNodeCount;
        ExpandedNodeCount = expandedNodeCount;
        HasExpandableNodes = hasExpandableNodes;
        CanExpandAny = canExpandAny;
        CanCollapseAny = canCollapseAny;
        this.expandAllAsync = expandAllAsync;
        this.collapseAllAsync = collapseAllAsync;
    }

    public int RootItemCount { get; }
    public int VisibleNodeCount { get; }
    public int ExpandedNodeCount { get; }
    public bool HasExpandableNodes { get; }
    public bool CanExpandAny { get; }
    public bool CanCollapseAny { get; }

    public Task ExpandAll() => expandAllAsync();
    public Task CollapseAll() => collapseAllAsync();
}
