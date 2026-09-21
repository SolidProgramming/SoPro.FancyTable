using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

namespace SoPro.FancyTable.Tests;

public sealed class PreparationTests : BunitContext
{
    public sealed class Row(string name)
    {
        public string Name { get; set; } = name;
        public List<Row> Children { get; } = [];
    }

    private int searches;
    private int sorts;
    private int children;
    private string prefix = "";

    private List<FancyColumn<Row>> Columns() =>
    [
        new() { Key = "name", Title = "Name", Sortable = true,
            ValueSelector = row => row.Name,
            SortValueSelector = row => { sorts++; return row.Name; } },
        new() { Key = "other", Title = "Other", Hideable = true, ValueSelector = row => row.Name }
    ];

    private bool Search(Row row, string filter)
    {
        searches++;
        return row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) && row.Name.StartsWith(prefix);
    }

    private IEnumerable<Row> Children(Row row)
    {
        children++;
        return row.Children;
    }

    private static void ApplySearch<T>(IRenderedComponent<T> cut, string text) where T : IComponent
    {
        cut.Find("input").Input(text);
        cut.Find("input").KeyUp(new KeyboardEventArgs { Key = "Enter" });
    }

    private static string[] Rows<T>(IRenderedComponent<T> cut) where T : IComponent =>
        cut.FindAll("tbody tr").Select(row => row.QuerySelector("td")!.TextContent.Trim()).ToArray();

    private static void Next<T>(IRenderedComponent<T> cut) where T : IComponent =>
        cut.FindAll(".fancy-pagination button").Last().Click();

    [Fact]
    public async Task Flat_display_actions_reuse_query_and_columns()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = Enumerable.Range(0, 8).Select(i => new Row($"row{i}")).ToList();
        var cut = Render<FancyTable<Row>>(p => p
            .Add(c => c.Items, items).Add(c => c.Columns, Columns())
            .Add(c => c.SearchPredicate, Search).Add(c => c.PaginationEnabled, true)
            .Add(c => c.PageSize, 2).Add(c => c.ShowPageSizeSelector, true)
            .Add(c => c.EnableColumnReordering, true));
        ApplySearch(cut, "row");
        cut.Find("th .btn-link").Click();
        int beforeSearch = searches, beforeSort = sorts;
        Next(cut);
        Assert.Equal(["row2", "row3"], Rows(cut));
        cut.Find(".bi-eye-slash").ParentElement!.Click();
        cut.Find(".bi-eye").ParentElement!.Click();
        cut.Find("select").Change("50");
        await cut.InvokeAsync(() => cut.Instance.OnColumnResizeCompleted([new("name", 150)]));
        await cut.InvokeAsync(() => cut.Instance.OnColumnReorderCompleted("other", "name", false));
        Assert.Equal(beforeSearch, searches);
        Assert.Equal(beforeSort, sorts);
        var rows = cut.FindComponents<FancyRow<Row>>();
        Assert.All(rows, row => Assert.Same(rows[0].Instance.VisibleColumns, row.Instance.VisibleColumns));
        Assert.Equal("other", rows[0].Instance.VisibleColumns[0].Key);
    }

    [Fact]
    public void Flat_parameter_delivery_refreshes_mutable_items_columns_and_closures()
    {
        var items = new List<Row> { new("alpha"), new("beta"), new("gamma") };
        var columns = Columns();
        var cut = Render<FancyTable<Row>>(p => p.Add(c => c.Items, items)
            .Add(c => c.Columns, columns).Add(c => c.SearchPredicate, Search)
            .Add(c => c.PaginationEnabled, true).Add(c => c.PageSize, 1));
        ApplySearch(cut, "a");
        cut.Find("th button").Click();
        Next(cut);
        Next(cut);
        items[0].Name = "zeta";
        items.RemoveAt(2);
        prefix = "z";
        cut.Render(p => p.Add(c => c.Items, items));
        Assert.Equal(["zeta"], Rows(cut));
        Assert.True(cut.FindAll(".fancy-pagination button").All(b => b.TextContent.Trim() != "3"));
        prefix = "";
        columns[0] = new() { Key = "name", Title = "Changed", Sortable = true,
            ValueSelector = r => r.Name, SortValueSelector = r => r.Name.Length };
        cut.Render(p => p.Add(c => c.Columns, columns));
        Assert.Equal(["zeta"], Rows(cut)); // Equal keys retain input order.
        cut.Find("th button").Click();
        ApplySearch(cut, "beta");
        Assert.Equal(["beta"], Rows(cut));
        items.Clear();
        cut.Render(p => p.Add(c => c.Items, items));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public async Task Tree_search_is_shared_and_display_actions_do_not_rebuild()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var root = new Row("root");
        root.Children.AddRange([new("match-b"), new("miss"), new("match-a")]);
        FancyTreeTableContext<Row>? context = null;
        RenderFragment<FancyTreeTableContext<Row>> capture = value => builder => context = value;
        var cut = Render<FancyTreeTable<Row>>(p => p.Add(c => c.Items, new[] { root })
            .Add(c => c.Columns, Columns()).Add(c => c.ChildItemsSelector, Children)
            .Add(c => c.SearchPredicate, Search).Add(c => c.TreeActionsTemplate, capture)
            .Add(c => c.PaginationEnabled, true).Add(c => c.PageSize, 2)
            .Add(c => c.EnableColumnReordering, true));
        ApplySearch(cut, "match");
        Assert.Equal(3, context!.VisibleNodeCount);
        int beforeSearch = searches, beforeChildren = children;
        Next(cut);
        Assert.Equal(["match-a"], Rows(cut));
        cut.Find(".bi-eye-slash").ParentElement!.Click();
        cut.Find(".bi-eye").ParentElement!.Click();
        await cut.InvokeAsync(() => cut.Instance.OnColumnResizeCompleted([new("name", 150)]));
        Assert.Equal(beforeSearch, searches);
        Assert.Equal(beforeChildren, children);
        // Search traverses each node once, even with both the pager and tree toolbar present.
        ApplySearch(cut, "match-a");
        Assert.Equal(4, searches - beforeSearch);
        Assert.Equal(["root", "match-a"], Rows(cut));
        ApplySearch(cut, "");
        Assert.Equal(["root"], Rows(cut));
        cut.Find(".tree-toggle-btn").Click();
        Assert.Equal(4, context.VisibleNodeCount);
        Assert.Equal(1, context.ExpandedNodeCount);
        await cut.InvokeAsync(() => cut.Instance.CollapseAllAsync());
        Assert.Equal(1, context.VisibleNodeCount);
        Assert.Equal(0, context.ExpandedNodeCount);
        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());
        Assert.Equal(4, context.VisibleNodeCount);
        Assert.Equal(beforeChildren, children);
        cut.Find("th .btn-link").Click();
        Assert.Equal(["root", "match-a"], Rows(cut));
        Assert.True(children > beforeChildren);
    }

    [Fact]
    public async Task Tree_refresh_handles_mutation_removal_nulls_and_cycles()
    {
        var root = new Row("root");
        var child = new Row("child");
        root.Children.AddRange([null!, child]);
        child.Children.Add(root);
        var items = new List<Row> { null!, root };
        var cut = Render<FancyTreeTable<Row>>(p => p.Add(c => c.Items, items)
            .Add(c => c.Columns, Columns()).Add(c => c.ChildItemsSelector, Children));
        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());
        Assert.Equal(["root", "child", "root"], Rows(cut));
        child.Name = "renamed";
        child.Children.Clear();
        cut.Render(p => p.Add(c => c.Items, items));
        // The cycle-ending leaf previously cleared the shared expansion state.
        Assert.Equal(["root"], Rows(cut));
        cut.Find(".tree-toggle-btn").Click();
        Assert.Equal(["root", "renamed"], Rows(cut));
        root.Children.Clear();
        cut.Render(p => p.Add(c => c.Items, items));
        Assert.Equal(["root"], Rows(cut));
        items.Clear();
        cut.Render(p => p.Add(c => c.Items, items));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public void Flat_sort_and_search_changes_refresh_results_and_summary()
    {
        FancyPaginationContext<Row>? summary = null;
        RenderFragment<FancyPaginationContext<Row>> capture = value => builder => summary = value;
        var cut = Render<FancyTable<Row>>(p => p
            .Add(c => c.Items, new[] { new Row("gamma"), new Row("alpha"), new Row("beta") })
            .Add(c => c.Columns, Columns()).Add(c => c.PaginationEnabled, true)
            .Add(c => c.PageSize, 2).Add(c => c.PaginationSummaryTemplate, capture));
        cut.Find("th button").Click();
        Assert.Equal(["alpha", "beta"], Rows(cut));
        cut.Find("th button").Click();
        Assert.Equal(["gamma", "beta"], Rows(cut));
        Next(cut);
        Assert.Equal(["alpha"], Rows(cut));
        Assert.Equal(2, summary!.CurrentPage);
        ApplySearch(cut, "beta");
        Assert.Equal(["beta"], Rows(cut));
        Assert.Equal(1, summary.CurrentPage);
        Assert.Equal(1, summary.FilteredItemCount);
        Assert.Equal(3, summary.TotalItemCount);
        ApplySearch(cut, "absent");
        Assert.Empty(Rows(cut));
        Assert.Equal(0, summary.FilteredItemCount);
        Assert.Equal(0, summary.StartItemIndex);
    }

    [Fact]
    public void Tree_parameter_delivery_refreshes_delegate_closures_and_search_semantics()
    {
        var root = new Row("parent");
        List<Row> childItems = [new("alpha"), new("beta")];
        var cut = Render<FancyTreeTable<Row>>(p => p.Add(c => c.Items, new[] { root })
            .Add(c => c.Columns, Columns()).Add(c => c.SearchPredicate, Search)
            .Add(c => c.ChildItemsSelector, row => row == root ? childItems : [])
            .Add(c => c.InitiallyExpandedSelector, _ => true));
        ApplySearch(cut, "parent");
        Assert.Equal(["parent"], Rows(cut)); // Matching parents don't automatically include children.
        ApplySearch(cut, "a");
        Assert.Equal(["parent", "alpha", "beta"], Rows(cut));
        prefix = "b";
        childItems = [new("bravo"), new("alpha")];
        cut.Render(p => p.Add(c => c.Items, new[] { root }));
        Assert.Equal(["parent", "bravo"], Rows(cut));
        ApplySearch(cut, "absent");
        Assert.Empty(Rows(cut));
        ApplySearch(cut, "");
        cut.Find("th button").Click();
        Assert.Equal(["parent", "alpha", "bravo"], Rows(cut));
        cut.Find("th button").Click();
        Assert.Equal(["parent", "bravo", "alpha"], Rows(cut));
        var rows = cut.FindComponents<FancyTreeRow<Row>>();
        Assert.All(rows, row => Assert.Same(rows[0].Instance.VisibleColumns, row.Instance.VisibleColumns));
    }
}
