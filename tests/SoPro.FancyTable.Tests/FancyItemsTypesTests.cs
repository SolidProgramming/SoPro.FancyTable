using System.Globalization;

[Collection("Culture")]
public sealed class FancyItemsTypesTests
{
    private static FancyPaginationContext<string> CreateContext(int pageCount, bool? hasNextPage)
    {
        return new FancyPaginationContext<string>(
            currentPage: 1,
            pageSize: 10,
            totalItemCount: 10,
            filteredItemCount: 10,
            pageCount: pageCount,
            startItemIndex: 1,
            endItemIndex: 10,
            goToPageAsync: _ => Task.CompletedTask,
            goToPreviousPageAsync: () => Task.CompletedTask,
            goToNextPageAsync: () => Task.CompletedTask,
            setPageSizeAsync: _ => Task.CompletedTask,
            hasNextPage: hasNextPage);
    }

    [Fact]
    public void PaginationContext_HasNextPageOverride_WinsOverPageCount()
    {
        Assert.True(CreateContext(pageCount: 1, hasNextPage: true).HasNextPage);
        Assert.False(CreateContext(pageCount: 3, hasNextPage: false).HasNextPage);
    }

    [Fact]
    public void PaginationContext_WithoutOverride_ComputesHasNextPageFromPageCount()
    {
        Assert.True(CreateContext(pageCount: 3, hasNextPage: null).HasNextPage);
        Assert.False(CreateContext(pageCount: 1, hasNextPage: null).HasNextPage);
    }

    [Fact]
    public void Texts_ShowingPageItemsFormat_IsLocalized()
    {
        Assert.Equal("Showing {0}-{1}", FancyTableTexts.Default.ShowingPageItemsFormat);
        Assert.Equal("Zeige {0}-{1}", FancyTableTexts.ForCulture(new CultureInfo("de")).ShowingPageItemsFormat);
    }

    [Fact]
    public void ItemsRequestAndResult_CarryTheirValues()
    {
        FancyColumnFilter filter = new("name", FancyColumnFilterOperator.Contains, "x");
        FancyItemsRequest request = new("search", "name", false, [filter], 2, 25);
        FancyItemsResult<string> result = new(["a"], HasMore: true);

        Assert.Equal(("search", "name", false, 2, 25), (request.SearchText, request.SortColumnKey, request.SortAscending, request.Page, request.PageSize));
        Assert.Same(filter, request.ColumnFilters.Single());
        Assert.True(result.HasMore);
        Assert.Equal("a", result.Items.Single());
    }
}
