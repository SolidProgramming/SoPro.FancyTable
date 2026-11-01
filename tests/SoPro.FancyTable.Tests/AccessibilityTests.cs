using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using SoPro.FancyTable.Components;
using Xunit;

public sealed class AccessibilityTests : BunitContext
{
    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public IReadOnlyList<Node> Children { get; } = children;
    }

    private static readonly List<Node> Nodes = [new Node("Alpha"), new Node("Beta")];

    private static readonly FancyColumnFilter[] NameFilter =
        [new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "a")];

    private static readonly FancyTableTexts CustomTexts = FancyTableTexts.Default with
    {
        SortColumnFormat = "Sortiere {0}",
        HideColumnFormat = "Verstecke {0}",
        FilteredColumnLabel = "gefiltert!",
        FilterOperatorLabel = "Vergleich",
        ClearSearchLabel = "Leeren",
        SearchPlaceholder = "Finden"
    };

    private const string NameHeader = "th[data-column-key='name']";
    private const string SortButton = NameHeader + " button[aria-label^='Sort']";

    public AccessibilityTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<Node>> Columns() =>
    [
        new FancyColumn<Node> { Key = "name", Title = "Name", ValueSelector = n => n.Name, Sortable = true, Hideable = true, Filterable = true }
    ];

    private IRenderedComponent<FancyTable<Node>> RenderTable(
        FancyTableTexts? texts = null,
        IReadOnlyList<FancyColumnFilter>? initialFilters = null)
    {
        return Render<FancyTable<Node>>(parameters => parameters
            .Add(p => p.Items, Nodes)
            .Add(p => p.Columns, Columns())
            .Add(p => p.Texts, texts)
            .Add(p => p.InitialColumnFilters, initialFilters));
    }

    private IRenderedComponent<FancyTreeTable<Node>> RenderTreeTable(
        FancyTableTexts? texts = null,
        IReadOnlyList<FancyColumnFilter>? initialFilters = null)
    {
        return Render<FancyTreeTable<Node>>(parameters => parameters
            .Add(p => p.Items, Nodes)
            .Add(p => p.Columns, Columns())
            .Add(p => p.ChildItemsSelector, n => n.Children)
            .Add(p => p.Texts, texts)
            .Add(p => p.InitialColumnFilters, initialFilters));
    }

    private static void AssertButton(IElement button, string label)
    {
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(label, button.GetAttribute("aria-label"));
        Assert.Equal("true", button.QuerySelector("i")!.GetAttribute("aria-hidden"));
    }

    private static void AssertAriaSort<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        Assert.False(cut.Find(NameHeader).HasAttribute("aria-sort"));

        cut.Find(SortButton).Click();
        Assert.Equal("ascending", cut.Find(NameHeader).GetAttribute("aria-sort"));

        cut.Find(SortButton).Click();
        Assert.Equal("descending", cut.Find(NameHeader).GetAttribute("aria-sort"));
    }

    private static void AssertSearchLabels<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        Assert.Equal("Search", cut.Find("input.form-control").GetAttribute("aria-label"));
        AssertButton(cut.Find("button[aria-label='Search']"), "Search");

        cut.Find("input.form-control").Input("x");
        AssertButton(cut.Find("button[aria-label='Clear search']"), "Clear search");
    }

    private static void AssertRestoreButton<T>(IRenderedComponent<T> plain, IRenderedComponent<T> filtered) where T : IComponent
    {
        plain.Find("th button[aria-label='Hide column Name']").Click();
        IElement plainRestore = plain.Find("div.mt-2 button");
        Assert.Equal("button", plainRestore.GetAttribute("type"));
        Assert.Equal("true", plainRestore.QuerySelector("i.bi-eye")!.GetAttribute("aria-hidden"));
        Assert.Empty(plain.FindAll("div.mt-2 .visually-hidden"));

        filtered.Find("th button[aria-label='Hide column Name']").Click();
        Assert.Equal("(filtered)", filtered.Find("div.mt-2 button .visually-hidden").TextContent);
    }

    private static void AssertCustomTexts<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        Assert.NotNull(cut.Find("th button[aria-label='Sortiere Name']"));
        Assert.NotNull(cut.Find("input.form-control[aria-label='Finden']"));
        cut.Find("input.form-control").Input("x");
        Assert.NotNull(cut.Find("button[aria-label='Leeren']"));
        cut.Find("th button[aria-label='Verstecke Name']").Click();
        Assert.Equal("(gefiltert!)", cut.Find("div.mt-2 button .visually-hidden").TextContent);
    }

    // FancyTable

    [Fact]
    public void Table_SortButton_HasTypeLabelAndHiddenIcon()
    {
        AssertButton(RenderTable().Find("th button[aria-label='Sort column Name']"), "Sort column Name");
    }

    [Fact]
    public void Table_AriaSort_FollowsSortState() => AssertAriaSort(RenderTable());

    [Fact]
    public void Table_HideButton_HasTypeLabelAndHiddenIcon()
    {
        AssertButton(RenderTable().Find("th button[aria-label='Hide column Name']"), "Hide column Name");
    }

    [Fact]
    public void Table_Search_HasLabels() => AssertSearchLabels(RenderTable());

    [Fact]
    public void Table_RestoreButton_FilteredMarkerOnlyWhenFiltered() =>
        AssertRestoreButton(RenderTable(), RenderTable(initialFilters: NameFilter));

    [Fact]
    public void Table_Labels_ComeFromTexts() => AssertCustomTexts(RenderTable(CustomTexts, NameFilter));

    // FancyTreeTable

    [Fact]
    public void Tree_SortButton_HasTypeLabelAndHiddenIcon()
    {
        AssertButton(RenderTreeTable().Find("th button[aria-label='Sort column Name']"), "Sort column Name");
    }

    [Fact]
    public void Tree_AriaSort_FollowsSortState() => AssertAriaSort(RenderTreeTable());

    [Fact]
    public void Tree_HideButton_HasTypeLabelAndHiddenIcon()
    {
        AssertButton(RenderTreeTable().Find("th button[aria-label='Hide column Name']"), "Hide column Name");
    }

    [Fact]
    public void Tree_Search_HasLabels() => AssertSearchLabels(RenderTreeTable());

    [Fact]
    public void Tree_RestoreButton_FilteredMarkerOnlyWhenFiltered() =>
        AssertRestoreButton(RenderTreeTable(), RenderTreeTable(initialFilters: NameFilter));

    [Fact]
    public void Tree_Labels_ComeFromTexts() => AssertCustomTexts(RenderTreeTable(CustomTexts, NameFilter));

    // Filter popup

    private IRenderedComponent<FancyColumnFilterPopup> RenderPopup(FancyTableTexts texts)
    {
        return Render<FancyColumnFilterPopup>(parameters => parameters
            .Add(p => p.ColumnKey, "name")
            .Add(p => p.ColumnTitle, "Name")
            .Add(p => p.Texts, texts));
    }

    [Fact]
    public void Popup_HasDialogRoleAndControlLabels()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(FancyTableTexts.Default);

        IElement popup = cut.Find("div.fancy-filter-popup");
        Assert.Equal("dialog", popup.GetAttribute("role"));
        Assert.Equal("Filter column Name", popup.GetAttribute("aria-label"));
        Assert.Equal("Operator", cut.Find("select").GetAttribute("aria-label"));
        Assert.Equal("Value", cut.Find("input").GetAttribute("aria-label"));
    }

    [Fact]
    public void Popup_Labels_ComeFromTexts()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(FancyTableTexts.Default with
        {
            FilterOperatorLabel = "Vergleich",
            FilterValuePlaceholder = "Wert",
            FilterColumnFormat = "Filter {0}"
        });

        Assert.Equal("Filter Name", cut.Find("div.fancy-filter-popup").GetAttribute("aria-label"));
        Assert.Equal("Vergleich", cut.Find("select").GetAttribute("aria-label"));
        Assert.Equal("Wert", cut.Find("input").GetAttribute("aria-label"));
    }
}
