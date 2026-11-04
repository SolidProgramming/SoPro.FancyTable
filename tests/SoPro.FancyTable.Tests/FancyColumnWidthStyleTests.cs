public sealed class FancyColumnWidthStyleTests
{
    private static FancyColumn<string> Column(double? minWidth = null, double? maxWidth = null) =>
        new() { Key = "name", Title = "Name", MinWidth = minWidth, MaxWidth = maxWidth };

    [Fact]
    public void GetWidthBoundsStyle_WithoutBounds_ReturnsNullSoBrowserSizesColumn()
    {
        Assert.Null(Column().GetWidthBoundsStyle());
    }

    [Fact]
    public void GetWidthBoundsStyle_WithMinWidth_ReturnsMinWidth()
    {
        Assert.Equal("min-width:80px;", Column(minWidth: 80).GetWidthBoundsStyle());
    }

    [Fact]
    public void GetWidthBoundsStyle_WithMaxWidth_ReturnsMaxWidth()
    {
        Assert.Equal("max-width:300.5px;", Column(maxWidth: 300.5).GetWidthBoundsStyle());
    }

    [Fact]
    public void GetWidthBoundsStyle_WithMinAndMaxWidth_ReturnsBoth()
    {
        Assert.Equal("min-width:80px;max-width:300px;", Column(minWidth: 80, maxWidth: 300).GetWidthBoundsStyle());
    }
}
