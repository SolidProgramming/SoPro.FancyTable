using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

[Collection("Culture")]
[UseEnglishCulture]
public sealed class FancyTreeTableServerModeTests : BunitContext
{
    private const int PageSize = 10;

    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public IReadOnlyList<Node> Children { get; } = children;
    }

    public FancyTreeTableServerModeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<Node>> Columns() =>
    [
        new FancyColumn<Node> { Key = "name", Title = "Name", ValueSelector = n => n.Name, Sortable = true, Filterable = true }
    ];

    private static FancyItemsResult<Node> Page(FancyItemsRequest request, bool hasMore = true) =>
        new([new Node($"root {request.Page}b", new Node("child z"), new Node("child a")), new Node($"root {request.Page}a")], hasMore);

    private IRenderedComponent<FancyTreeTable<Node>> RenderServerTree(FakeItemsProvider<Node> provider,
        Func<Node, bool>? initiallyExpanded = null, RenderFragment? loadingTemplate = null)
    {
        return Render<FancyTreeTable<Node>>(parameters => parameters
            .Add(p => p.ItemsProvider, provider.LoadAsync)
            .Add(p => p.Columns, Columns())
            .Add(p => p.ChildItemsSelector, n => n.Children)
            .Add(p => p.InitiallyExpandedSelector, initiallyExpanded)
            .Add(p => p.PaginationEnabled, true)
            .Add(p => p.PageSize, PageSize)
            .Add(p => p.ShowPageSizeSelector, true)
            .Add(p => p.LoadingTemplate, loadingTemplate));
    }

    private static IReadOnlyList<string> Rows(IRenderedComponent<FancyTreeTable<Node>> cut) =>
        cut.FindAll("tbody tr").Select(row => row.TextContent.Trim()).ToList();

    [Fact]
    public void FirstRender_CallsProviderWithPage1AndShowsRootsInProviderOrder()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));

        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);

        FancyItemsRequest request = Assert.Single(provider.Requests);
        Assert.Equal((1, PageSize, "", (string?)null), (request.Page, request.PageSize, request.SearchText, request.SortColumnKey));
        Assert.Equal(2, Rows(cut).Count);
        Assert.Contains("root 1b", Rows(cut)[0]);
    }

    [Fact]
    public async Task Children_KeepProviderOrder_NoSiblingSort()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        cut.Find("th[data-column-key='name'] button[aria-label^='Sort']").Click();

        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());

        IReadOnlyList<string> rows = Rows(cut);
        Assert.Equal(4, rows.Count);
        Assert.Contains("root 1b", rows[0]);
        Assert.Contains("child z", rows[1]);
        Assert.Contains("child a", rows[2]);
    }

    [Fact]
    public void SortClick_CallsProviderWithSortKeyAndResetsToPage1()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        cut.Find("button:contains('Next')").Click();

        cut.Find("th[data-column-key='name'] button[aria-label^='Sort']").Click();

        Assert.Equal(("name", true, 1), (provider.LastRequest.SortColumnKey, provider.LastRequest.SortAscending, provider.LastRequest.Page));
    }

    [Fact]
    public void Search_AppliedOnEnter_CallsProviderWithSearchTextAndDoesNotFilterLocally()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);

        cut.Find("input[placeholder]").Input("zzz");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("zzz", provider.LastRequest.SearchText);
        Assert.Equal(2, Rows(cut).Count);
    }

    [Fact]
    public void ColumnFilter_Applied_CallsProviderWithFilters()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);

        cut.Find("th[data-column-key='name'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='name'] select").Change(nameof(FancyColumnFilterOperator.Contains));
        cut.Find("th[data-column-key='name'] input").Input("x");
        cut.Find("th[data-column-key='name'] button.btn-primary").Click();

        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "x"), Assert.Single(provider.LastRequest.ColumnFilters));
    }

    [Fact]
    public void NextPage_CallsProviderWithPage2_AndNextDisabledWhenHasMoreFalse()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r, hasMore: r.Page < 2));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);

        cut.Find("button:contains('Next')").Click();

        Assert.Equal(2, provider.LastRequest.Page);
        Assert.True(cut.Find("button:contains('Next')").HasAttribute("disabled"));
        Assert.Contains("Showing 11-12", cut.Find(".fancy-pagination-summary").TextContent);
    }

    [Fact]
    public void PageSizeChange_ResetsToPage1()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        cut.Find("button:contains('Next')").Click();

        cut.Find(".fancy-page-size select").Change("25");

        Assert.Equal((1, 25), (provider.LastRequest.Page, provider.LastRequest.PageSize));
    }

    [Fact]
    public async Task StaleResult_IsDiscarded()
    {
        FakeItemsProvider<Node> provider = new(r => new FancyItemsResult<Node>([new Node($"result {r.SearchText}")], false));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        TaskCompletionSource slowCall = new();
        provider.Gate = slowCall;

        cut.Find("input[placeholder]").Input("old");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("input[placeholder]").Input("new");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        await cut.InvokeAsync(() => slowCall.SetResult());

        Assert.True(provider.Tokens[1].IsCancellationRequested);
        cut.WaitForAssertion(() => Assert.Contains("result new", Assert.Single(Rows(cut))));
    }

    [Fact]
    public void ProviderException_Propagates()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r)) { ExceptionToThrow = new InvalidOperationException("boom") };

        Assert.Equal("boom", Assert.Throws<InvalidOperationException>(() => RenderServerTree(provider)).Message);
    }

    [Fact]
    public void ItemsAndItemsProvider_Both_Throw()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));

        Assert.Throws<InvalidOperationException>(() => Render<FancyTreeTable<Node>>(parameters => parameters
            .Add(p => p.Items, [new Node("a")])
            .Add(p => p.ItemsProvider, provider.LoadAsync)
            .Add(p => p.Columns, Columns())
            .Add(p => p.ChildItemsSelector, n => n.Children)));
    }

    [Fact]
    public async Task Refresh_KeepsPage_ResetToFirstPage_GoesToPage1()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        cut.Find("button:contains('Next')").Click();

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());
        Assert.Equal(2, provider.LastRequest.Page);

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync(resetToFirstPage: true));
        Assert.Equal(1, provider.LastRequest.Page);
    }

    [Fact]
    public async Task Refresh_EmptyPageAfterFirst_StepsBackOnePage()
    {
        bool pageTwoEmptied = false;
        FakeItemsProvider<Node> provider = new(r => pageTwoEmptied && r.Page == 2
            ? new FancyItemsResult<Node>([], false)
            : Page(r, hasMore: r.Page < 2));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider);
        cut.Find("button:contains('Next')").Click();
        pageTwoEmptied = true;

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        Assert.Equal(1, provider.LastRequest.Page);
        Assert.Contains("root 1b", Rows(cut)[0]);
    }

    [Fact]
    public async Task AfterReload_NodesCollapsed_UnlessInitiallyExpanded()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r));
        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider, initiallyExpanded: n => n.Name == "root 2b");
        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());
        Assert.Equal(4, Rows(cut).Count);

        cut.Find("button:contains('Next')").Click();

        Assert.Equal(4, Rows(cut).Count);
        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync(resetToFirstPage: true));
        Assert.Equal(2, Rows(cut).Count);
    }

    [Fact]
    public async Task Loading_SetsAriaBusyAndRendersLoadingTemplate()
    {
        FakeItemsProvider<Node> provider = new(r => Page(r)) { Gate = new TaskCompletionSource() };
        TaskCompletionSource gate = provider.Gate;

        IRenderedComponent<FancyTreeTable<Node>> cut = RenderServerTree(provider,
            loadingTemplate: builder => builder.AddMarkupContent(0, "<span class='test-loading'>loading</span>"));

        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
        Assert.Single(cut.FindAll(".test-loading"));

        await cut.InvokeAsync(() => gate.SetResult());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".test-loading")));
        Assert.Equal("false", cut.Find("table").GetAttribute("aria-busy"));
    }
}
