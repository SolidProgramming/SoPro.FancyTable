using Bunit;
using SoPro.FancyTable.Components;
using Xunit;

/// <summary>
/// The top bar layout defaults live in the component CSS, not as Bootstrap utility classes in the
/// markup, so utility classes passed by consumers are never overridden by built-in ones.
/// </summary>
public sealed class TopBarLayoutTests : BunitContext
{
    public TopBarLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<string>> Columns() =>
    [
        new FancyColumn<string> { Key = "name", Title = "Name", ValueSelector = s => s }
    ];

    [Fact]
    public void FancyTable_TopBarContainers_HaveSemanticClassesAndConsumerClassesOnly()
    {
        IRenderedComponent<FancyTable<string>> cut = Render<FancyTable<string>>(parameters => parameters
            .Add(p => p.Items, ["a", "b"])
            .Add(p => p.Columns, Columns())
            .Add(p => p.PaginationEnabled, true)
            .Add(p => p.ShowPageSizeSelector, true)
            .Add(p => p.TopBarClass, "justify-content-start")
            .Add(p => p.PageSizeContainerClass, "d-inline-flex")
            .Add(p => p.ToolbarContainerClass, "d-inline-flex ms-0"));

        AssertClasses(cut.Find(".fancy-table-topbar").ClassList, "fancy-table-topbar", "justify-content-start");
        AssertClasses(cut.Find(".fancy-page-size").ClassList, "fancy-page-size", "d-inline-flex");
        AssertClasses(cut.Find(".fancy-toolbar").ClassList, "fancy-toolbar", "d-inline-flex", "ms-0");
    }

    [Fact]
    public void FancyTreeTable_TopBarContainers_HaveSemanticClassesAndConsumerClassesOnly()
    {
        IRenderedComponent<FancyTreeTable<string>> cut = Render<FancyTreeTable<string>>(parameters => parameters
            .Add(p => p.Items, ["a", "b"])
            .Add(p => p.Columns, Columns())
            .Add(p => p.ChildItemsSelector, _ => [])
            .Add(p => p.PaginationEnabled, true)
            .Add(p => p.ShowPageSizeSelector, true)
            .Add(p => p.TopBarClass, "justify-content-start")
            .Add(p => p.PageSizeContainerClass, "d-inline-flex")
            .Add(p => p.ToolbarContainerClass, "d-inline-flex ms-0"));

        AssertClasses(cut.Find(".fancy-table-topbar").ClassList, "fancy-table-topbar", "justify-content-start");
        AssertClasses(cut.Find(".fancy-page-size").ClassList, "fancy-page-size", "d-inline-flex");
        AssertClasses(cut.Find(".fancy-toolbar").ClassList, "fancy-toolbar", "d-inline-flex", "ms-0");
    }

    [Fact]
    public void FancyTable_DefaultToolbar_HasNoBuiltInLayoutUtilities()
    {
        IRenderedComponent<FancyTable<string>> cut = Render<FancyTable<string>>(parameters => parameters
            .Add(p => p.Items, ["a"])
            .Add(p => p.Columns, Columns()));

        AssertClasses(cut.Find(".fancy-table-topbar").ClassList, "fancy-table-topbar");
        AssertClasses(cut.Find(".fancy-toolbar").ClassList, "fancy-toolbar");
    }

    // Exact match: no built-in layout utility (d-flex, ms-auto, justify-content-between, ...) may sneak back in.
    private static void AssertClasses(IEnumerable<string> actual, params string[] expected) =>
        Assert.Equal(expected.OrderBy(c => c), actual.OrderBy(c => c));
}
