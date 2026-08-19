using Microsoft.AspNetCore.Components;

public sealed class FancyTreeRowTemplateContext<TItem>
{
    public required TreeNodeState<TItem> Node { get; init; }
    public required EventCallback<TreeNodeState<TItem>> ToggleNode { get; init; }
    public required string ExpandLabel { get; init; }
    public required string CollapseLabel { get; init; }
}
