/// <summary>
/// Builds the entries of the page number buttons: a window of pages around the current page, always together with the
/// first and the last page, and a gap (<c>null</c>) where pages between them are left out, e.g. 1 … 4 5 6 7 8 … 100.
/// </summary>
internal static class FancyPagerPages
{
    private const int WindowSize = 5;
    private const int PagesBeforeCurrent = 2;

    /// <summary>
    /// Returns the page numbers to show in order; <c>null</c> stands for left out pages.
    /// </summary>
    public static IReadOnlyList<int?> Get(int currentPage, int pageCount)
    {
        if (pageCount < 1)
        {
            return [];
        }

        int startPage = Math.Max(1, currentPage - PagesBeforeCurrent);
        int endPage = Math.Min(pageCount, startPage + WindowSize - 1);
        if ((endPage - startPage) < WindowSize - 1)
        {
            startPage = Math.Max(1, endPage - WindowSize + 1);
        }

        List<int?> pages = [];
        if (startPage > 1)
        {
            pages.Add(1);
            if (startPage > 2)
            {
                pages.Add(null);
            }
        }

        pages.AddRange(Enumerable.Range(startPage, endPage - startPage + 1).Select(page => (int?)page));

        if (endPage < pageCount)
        {
            if (endPage < pageCount - 1)
            {
                pages.Add(null);
            }
            pages.Add(pageCount);
        }

        return pages;
    }
}
