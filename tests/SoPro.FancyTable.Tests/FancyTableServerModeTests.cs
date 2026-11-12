using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

[Collection("Culture")]
[UseEnglishCulture]
public sealed class FancyTableServerModeTests : BunitContext
{
    private const int PageSize = 10;

    public FancyTableServerModeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<string>> Columns() =>
    [
        new FancyColumn<string> { Key = "name", Title = "Name", ValueSelector = s => s, Sortable = true, Filterable = true }
    ];

    private static FancyItemsResult<string> Page(FancyItemsRequest request, bool hasMore = true) =>
        new([$"row {request.Page}a", $"row {request.Page}b"], hasMore);

    private IRenderedComponent<FancyTable<string>> RenderServerTable(
        FakeItemsProvider<string> provider,
        RenderFragment? loadingTemplate = null,
        List<FancyColumn<string>>? columns = null)
    {
        return Render<FancyTable<string>>(parameters => parameters
            .Add(p => p.ItemsProvider, provider.LoadAsync)
            .Add(p => p.Columns, columns ?? Columns())
            .Add(p => p.PaginationEnabled, true)
            .Add(p => p.PageSize, PageSize)
            .Add(p => p.ShowPageSizeSelector, true)
            .Add(p => p.LoadingTemplate, loadingTemplate));
    }

    private static IReadOnlyList<string> Rows(IRenderedComponent<FancyTable<string>> cut) =>
        cut.FindAll("tbody tr").Select(row => row.TextContent.Trim()).ToList();

    [Fact]
    public void FirstRender_CallsProviderWithPage1AndDefaults()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));

        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);

        FancyItemsRequest request = Assert.Single(provider.Requests);
        Assert.Equal(new FancyItemsRequest("", null, true, request.ColumnFilters, 1, PageSize), request);
        Assert.Empty(request.ColumnFilters);
        Assert.Equal(["row 1a", "row 1b"], Rows(cut));
    }

    [Fact]
    public void SortClick_CallsProviderWithSortKeyAndResetsToPage1()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
        cut.Find("button:contains('Next')").Click();

        cut.Find("th[data-column-key='name'] button[aria-label^='Sort']").Click();
        Assert.Equal(("name", true, 1), (provider.LastRequest.SortColumnKey, provider.LastRequest.SortAscending, provider.LastRequest.Page));

        cut.Find("th[data-column-key='name'] button[aria-label^='Sort']").Click();
        Assert.False(provider.LastRequest.SortAscending);
    }

    [Fact]
    public void Search_AppliedOnEnter_CallsProviderWithSearchTextOnPage1()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
        cut.Find("button:contains('Next')").Click();

        cut.Find("input[placeholder]").Input("abc");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(("abc", 1), (provider.LastRequest.SearchText, provider.LastRequest.Page));
    }

    [Fact]
    public void ColumnFilter_Applied_CallsProviderWithFilters()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);

        cut.Find("th[data-column-key='name'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='name'] select").Change(nameof(FancyColumnFilterOperator.Contains));
        cut.Find("th[data-column-key='name'] input").Input("x");
        cut.Find("th[data-column-key='name'] button.btn-primary").Click();

        FancyColumnFilter filter = Assert.Single(provider.LastRequest.ColumnFilters);
        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "x"), filter);
        Assert.Equal(1, provider.LastRequest.Page);
    }

    [Fact]
    public void NextPage_CallsProviderWithPage2_AndNextDisabledWhenHasMoreFalse()
    {
        FakeItemsProvider<string> provider = new(r => Page(r, hasMore: r.Page < 2));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);

        cut.Find("button:contains('Next')").Click();

        Assert.Equal(2, provider.LastRequest.Page);
        Assert.Equal(["row 2a", "row 2b"], Rows(cut));
        Assert.True(cut.Find("button:contains('Next')").HasAttribute("disabled"));
        Assert.False(cut.Find("button:contains('Previous')").HasAttribute("disabled"));
        Assert.Contains("Showing 11-12", cut.Find(".fancy-pagination-summary").TextContent);
    }

    [Fact]
    public void PageSizeChange_ResetsToPage1()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
        cut.Find("button:contains('Next')").Click();

        cut.Find(".fancy-page-size select").Change("25");

        Assert.Equal((1, 25), (provider.LastRequest.Page, provider.LastRequest.PageSize));
    }

    [Fact]
    public void RowsShownAsReturned_NoLocalSortOrSearch()
    {
        FakeItemsProvider<string> provider = new(_ => new FancyItemsResult<string>(["b", "a"], false));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);

        cut.Find("input[placeholder]").Input("zzz");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("th[data-column-key='name'] button[aria-label^='Sort']").Click();

        Assert.Equal(["b", "a"], Rows(cut));
    }

    [Fact]
    public async Task StaleResult_IsDiscarded()
    {
        FakeItemsProvider<string> provider = new(r => new FancyItemsResult<string>([r.SearchText == "" ? "initial" : $"result {r.SearchText}"], false));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
        TaskCompletionSource slowCall = new();
        provider.Gate = slowCall;

        cut.Find("input[placeholder]").Input("old");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("input[placeholder]").Input("new");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        await cut.InvokeAsync(() => slowCall.SetResult());

        Assert.True(provider.Tokens[1].IsCancellationRequested);
        cut.WaitForAssertion(() => Assert.Equal(["result new"], Rows(cut)));
    }

    [Fact]
    public void ProviderException_Propagates()
    {
        FakeItemsProvider<string> provider = new(r => Page(r)) { ExceptionToThrow = new InvalidOperationException("boom") };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => RenderServerTable(provider));

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public void ItemsAndItemsProvider_Both_Throw()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));

        Assert.Throws<InvalidOperationException>(() => Render<FancyTable<string>>(parameters => parameters
            .Add(p => p.Items, ["a"])
            .Add(p => p.ItemsProvider, provider.LoadAsync)
            .Add(p => p.Columns, Columns())));
    }

    [Fact]
    public async Task Refresh_KeepsPage_ResetToFirstPage_GoesToPage1()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
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
        FakeItemsProvider<string> provider = new(r => pageTwoEmptied && r.Page == 2
            ? new FancyItemsResult<string>([], false)
            : Page(r, hasMore: r.Page < 2));
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider);
        cut.Find("button:contains('Next')").Click();
        pageTwoEmptied = true;

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        Assert.Equal(1, provider.LastRequest.Page);
        Assert.Equal(["row 1a", "row 1b"], Rows(cut));
    }

    [Fact]
    public async Task Loading_SetsAriaBusyAndRendersLoadingTemplate()
    {
        FakeItemsProvider<string> provider = new(r => Page(r)) { Gate = new TaskCompletionSource() };
        TaskCompletionSource gate = provider.Gate;

        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider, builder => builder.AddMarkupContent(0, "<span class='test-loading'>loading</span>"));

        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
        Assert.Single(cut.FindAll(".test-loading"));

        await cut.InvokeAsync(() => gate.SetResult());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".test-loading")));
        Assert.Equal("false", cut.Find("table").GetAttribute("aria-busy"));
    }

    [Fact]
    public void BetweenFilter_ReachesProviderWithValueTo()
    {
        FakeItemsProvider<string> provider = new(r => Page(r));
        List<FancyColumn<string>> columns =
        [
            new FancyColumn<string> { Key = "n", Title = "Length", ValueSelector = s => s.Length, Filterable = true, FilterType = FancyColumnFilterType.Number }
        ];
        IRenderedComponent<FancyTable<string>> cut = RenderServerTable(provider, columns: columns);

        cut.Find("th[data-column-key='n'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='n'] select").Change(nameof(FancyColumnFilterOperator.Between));
        cut.Find("th[data-column-key='n'] input[aria-label='From']").Input("1");
        cut.Find("th[data-column-key='n'] input[aria-label='To']").Input("5");
        cut.Find("th[data-column-key='n'] button.btn-primary").Click();

        Assert.Equal(new FancyColumnFilter("n", FancyColumnFilterOperator.Between, "1", "5"), Assert.Single(provider.LastRequest.ColumnFilters));
    }
}
