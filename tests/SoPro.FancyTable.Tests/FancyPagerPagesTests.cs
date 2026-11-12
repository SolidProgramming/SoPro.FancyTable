using Xunit;

public sealed class FancyPagerPagesTests
{
    private static string Pages(int currentPage, int pageCount) =>
        string.Join(" ", FancyPagerPages.Get(currentPage, pageCount).Select(page => page?.ToString() ?? "…"));

    [Theory]
    [InlineData(1, 0, "")]
    [InlineData(1, 1, "1")]
    [InlineData(1, 5, "1 2 3 4 5")]
    [InlineData(3, 6, "1 2 3 4 5 6")]
    [InlineData(1, 100, "1 2 3 4 5 … 100")]
    [InlineData(4, 100, "1 2 3 4 5 6 … 100")]
    [InlineData(5, 100, "1 … 3 4 5 6 7 … 100")]
    [InlineData(50, 100, "1 … 48 49 50 51 52 … 100")]
    [InlineData(97, 100, "1 … 95 96 97 98 99 100")]
    [InlineData(100, 100, "1 … 96 97 98 99 100")]
    public void Get_ShowsWindowWithFirstAndLastPageAndGaps(int currentPage, int pageCount, string expected)
    {
        Assert.Equal(expected, Pages(currentPage, pageCount));
    }
}
