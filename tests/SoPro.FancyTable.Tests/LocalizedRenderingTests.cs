using System.Globalization;
using Bunit;
using SoPro.FancyTable.Components;

[Collection("Culture")]
public sealed class LocalizedRenderingTests : BunitContext
{
    private sealed record Person(string Name);

    private static readonly List<Person> People = [new("Anna"), new("Bob")];

    private static readonly List<FancyColumn<Person>> Columns =
    [
        new FancyColumn<Person> { Key = "name", Title = "Name", ValueSelector = p => p.Name, Filterable = true }
    ];

    public LocalizedRenderingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static void WithGermanUICulture(Action action)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de");
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    private IRenderedComponent<FancyTable<Person>> RenderTable(FancyTableTexts? texts = null)
    {
        return Render<FancyTable<Person>>(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.Columns, Columns)
            .Add(p => p.Texts, texts));
    }

    [Fact]
    public void FancyTable_WithoutTexts_RendersGermanUnderGermanCulture()
    {
        WithGermanUICulture(() =>
        {
            IRenderedComponent<FancyTable<Person>> cut = RenderTable();

            Assert.Equal("Suchen", cut.Find("input.form-control").GetAttribute("placeholder"));
            Assert.Equal("Spalte Name filtern", cut.Find("button.fancy-filter-button").GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void FancyTreeTable_WithoutTexts_RendersGermanUnderGermanCulture()
    {
        WithGermanUICulture(() =>
        {
            IRenderedComponent<FancyTreeTable<Person>> cut = Render<FancyTreeTable<Person>>(parameters => parameters
                .Add(p => p.Items, People)
                .Add(p => p.Columns, Columns)
                .Add(p => p.ChildItemsSelector, _ => Array.Empty<Person>()));

            Assert.Equal("Suchen", cut.Find("input.form-control").GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void FancyTable_WithDefaultTexts_StaysEnglishUnderGermanCulture()
    {
        WithGermanUICulture(() =>
        {
            IRenderedComponent<FancyTable<Person>> cut = RenderTable(FancyTableTexts.Default);

            Assert.Equal("Search", cut.Find("input.form-control").GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void FilterPopup_UnderGermanCulture_ShowsGermanOperators()
    {
        WithGermanUICulture(() =>
        {
            IRenderedComponent<FancyTable<Person>> cut = RenderTable();

            cut.Find("button.fancy-filter-button").Click();

            Assert.Contains(cut.FindAll("select option"), option => option.TextContent.Trim() == "Enthält");
        });
    }
}
