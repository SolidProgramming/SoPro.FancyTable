using Microsoft.AspNetCore.Components;

public sealed class FancyPaginationContext<TItem>
{
    private readonly Func<int, Task> goToPageAsync;
    private readonly Func<Task> goToPreviousPageAsync;
    private readonly Func<Task> goToNextPageAsync;
    private readonly Func<int, Task> setPageSizeAsync;

    public FancyPaginationContext(
        int currentPage,
        int pageSize,
        int totalItemCount,
        int filteredItemCount,
        int pageCount,
        int startItemIndex,
        int endItemIndex,
        Func<int, Task> goToPageAsync,
        Func<Task> goToPreviousPageAsync,
        Func<Task> goToNextPageAsync,
        Func<int, Task> setPageSizeAsync)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalItemCount = totalItemCount;
        FilteredItemCount = filteredItemCount;
        PageCount = pageCount;
        StartItemIndex = startItemIndex;
        EndItemIndex = endItemIndex;
        this.goToPageAsync = goToPageAsync;
        this.goToPreviousPageAsync = goToPreviousPageAsync;
        this.goToNextPageAsync = goToNextPageAsync;
        this.setPageSizeAsync = setPageSizeAsync;
    }

    public int CurrentPage { get; }
    public int PageSize { get; }
    public int TotalItemCount { get; }
    public int FilteredItemCount { get; }
    public int PageCount { get; }
    public int StartItemIndex { get; }
    public int EndItemIndex { get; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < PageCount;

    public Task GoToPage(int page) => goToPageAsync(page);
    public Task GoToPreviousPage() => goToPreviousPageAsync();
    public Task GoToNextPage() => goToNextPageAsync();
    public Task SetPageSize(int pageSize) => setPageSizeAsync(pageSize);
}
