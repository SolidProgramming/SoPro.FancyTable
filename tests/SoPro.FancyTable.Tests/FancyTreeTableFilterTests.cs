using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

public sealed class FancyTreeTableFilterTests : BunitContext
{
    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public IReadOnlyList<Node> Children { get; } = children;
    }

    private static readonly List<Node> Roots =
    [
        new Node("Root A", new Node("Child A1", new Node("Target"))),
        new Node("Root B", new Node("Child B1"))
    ];

    public FancyTreeTableFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<Node>> Columns(bool hideableName = false)
    {
        return
        [
            new FancyColumn<Node> { Key = "name", Title = "Name", ValueSelector = n => n.Name, Filterable = true, Hideable = hideableName }
        ];
    }

    private IRenderedComponent<FancyTreeTable<Node>> RenderTreeTable(
        IReadOnlyList<FancyColumnFilter>? initial = null,
        Action<IReadOnlyList<FancyColumnFilter>>? onChanged = null,
        bool paginate = false,
        int currentPage = 1,
        Action<int>? onPageChanged = null,
        Func<Node, string, bool>? searchPredicate = null,
        bool enableReordering = false)
    {
        return Render<FancyTreeTable<Node>>(parameters =>
        {
            parameters
                .Add(p => p.Items, Roots)
                .Add(p => p.Columns, Columns())
                .Add(p => p.ChildItemsSelector, n => n.Children)
                .Add(p => p.EnableColumnReordering, enableReordering)
                .Add(p => p.InitialColumnFilters, initial)
                .Add(p => p.ColumnFiltersChanged, EventCallback.Factory.Create<IReadOnlyList<FancyColumnFilter>>(this, f => onChanged?.Invoke(f)))
                .Add(p => p.SearchPredicate, searchPredicate);
            if (paginate)
            {
                parameters
                    .Add(p => p.PaginationEnabled, true)
                    .Add(p => p.PageSize, 1)
                    .Add(p => p.CurrentPage, currentPage)
                    .Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, page => onPageChanged?.Invoke(page)));
            }
        });
    }

    private static void ApplyFilter(IRenderedComponent<FancyTreeTable<Node>> cut, string key, string operatorName, string? value)
    {
        string th = $"th[data-column-key='{key}']";
        cut.Find($"{th} button.fancy-filter-button").Click();
        cut.Find($"{th} select").Change(operatorName);
        if (value is not null)
        {
            cut.Find($"{th} input").Input(value);
        }

        cut.Find($"{th} button.btn-primary").Click();
    }

    private static string[] RowTexts(IRenderedComponent<FancyTreeTable<Node>> cut) =>
        cut.FindAll("tbody tr").Select(row => row.TextContent.Trim()).ToArray();

    [Fact]
    public void FilterOnly_NoSearch_ShowsMatchWithAncestors()
    {
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(
            initial: [new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Target")]);

        string[] rows = RowTexts(cut);

        Assert.Equal(3, rows.Length);
        Assert.Contains("Root A", rows[0]);
        Assert.Contains("Child A1", rows[1]);
        Assert.Contains("Target", rows[2]);
    }

    [Fact]
    public void FilterAndSearch_NodeMustSatisfyBoth()
    {
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(
            initial: [new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "Child")]);
        Assert.Equal(4, RowTexts(cut).Length);

        cut.Find("input[placeholder]").Input("B1");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });

        string[] rows = RowTexts(cut);
        Assert.Equal(2, rows.Length);
        Assert.Contains("Root B", rows[0]);
        Assert.Contains("Child B1", rows[1]);
    }

    [Fact]
    public void CustomSearchPredicate_NotCalledWhenOnlyFiltersActive()
    {
        int calls = 0;
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(searchPredicate: (_, _) =>
        {
            calls++;
            return true;
        });

        ApplyFilter(cut, "name", "Equals", "Target");

        Assert.Equal(3, RowTexts(cut).Length);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Apply_RaisesChangedAndResetsPage()
    {
        IReadOnlyList<FancyColumnFilter>? received = null;
        List<int> pages = [];
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(
            onChanged: f => received = f,
            paginate: true,
            currentPage: 2,
            onPageChanged: pages.Add);
        Assert.Contains("Root B", cut.Find("tbody tr").TextContent);

        ApplyFilter(cut, "name", "Equals", "Target");

        Assert.Equal([new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Target")], received);
        Assert.Equal([1], pages);
        Assert.Contains("Root A", cut.Find("tbody tr").TextContent);
    }

    [Fact]
    public void Clear_RestoresExpandedView()
    {
        IReadOnlyList<FancyColumnFilter>? received = null;
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(
            initial: [new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Target")],
            onChanged: f => received = f);
        Assert.Equal(3, RowTexts(cut).Length);

        cut.Find("th[data-column-key='name'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='name'] button.btn-outline-secondary").Click();

        Assert.NotNull(received);
        Assert.Empty(received);
        string[] rows = RowTexts(cut);
        Assert.Equal(2, rows.Length);
        Assert.Contains("Root A", rows[0]);
        Assert.Contains("Root B", rows[1]);
    }

    [Fact]
    public void PinnedTreeColumn_CanBeFiltered()
    {
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderTreeTable(enableReordering: true);

        Assert.Single(cut.FindAll("th[data-column-key='name'] button.fancy-filter-button"));
    }

    [Fact]
    public void HiddenFilteredColumn_RestoreButtonShowsFunnel()
    {
        IRenderedComponent<FancyTreeTable<Node>> cut = Render<FancyTreeTable<Node>>(parameters => parameters
            .Add(p => p.Items, Roots)
            .Add(p => p.Columns,
            [
                new FancyColumn<Node> { Key = "name", Title = "Name", ValueSelector = n => n.Name },
                new FancyColumn<Node> { Key = "other", Title = "Other", ValueSelector = n => n.Name, Filterable = true, Hideable = true }
            ])
            .Add(p => p.ChildItemsSelector, n => n.Children)
            .Add(p => p.InitialColumnFilters, [new FancyColumnFilter("other", FancyColumnFilterOperator.Equals, "Target")]));

        cut.Find("th[data-column-key='other'] button:has(i.bi-eye-slash)").Click();

        Assert.Single(cut.FindAll("button:has(i.bi-eye) i.bi-funnel-fill"));
    }

    private sealed class DatedNode(string name, DateTime day, params DatedNode[] children)
    {
        public string Name { get; } = name;
        public DateTime Day { get; } = day;
        public IReadOnlyList<DatedNode> Children { get; } = children;
    }

    [Fact]
    public void DateColumn_BetweenFilter_KeepsMatchingChildAndAncestors()
    {
        List<DatedNode> roots =
        [
            new DatedNode("Root A", new DateTime(2020, 1, 1), new DatedNode("Child A1", new DateTime(2026, 6, 15, 9, 0, 0))),
            new DatedNode("Root B", new DateTime(2020, 1, 1), new DatedNode("Child B1", new DateTime(2021, 1, 1)))
        ];
        IRenderedComponent<FancyTreeTable<DatedNode>> cut = Render<FancyTreeTable<DatedNode>>(parameters => parameters
            .Add(p => p.Items, roots)
            .Add(p => p.Columns,
            [
                new FancyColumn<DatedNode> { Key = "name", Title = "Name", ValueSelector = n => n.Name },
                new FancyColumn<DatedNode> { Key = "day", Title = "Day", ValueSelector = n => n.Day, Filterable = true, FilterType = FancyColumnFilterType.Date }
            ])
            .Add(p => p.ChildItemsSelector, n => n.Children)
            .Add(p => p.InitialColumnFilters, [new FancyColumnFilter("day", FancyColumnFilterOperator.Between, "2026-06-01", "2026-06-30")]));

        List<string> rows = cut.FindAll("tbody tr").Select(row => row.TextContent).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains("Root A", rows[0]);
        Assert.Contains("Child A1", rows[1]);
    }
}
