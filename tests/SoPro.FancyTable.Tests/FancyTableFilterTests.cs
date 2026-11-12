using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

public sealed class FancyTableFilterTests : BunitContext
{
    private sealed record Person(int Id, string Name, string City);

    private static readonly List<Person> People = Enumerable.Range(1, 30)
        .Select(i => new Person(i, i % 2 == 0 ? $"Anna {i}" : $"Bob {i}", i % 3 == 0 ? "Berlin" : "Hamburg"))
        .ToList();

    public FancyTableFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<FancyColumn<Person>> Columns(bool filterable = true, bool hideableCity = false)
    {
        return
        [
            new FancyColumn<Person> { Key = "name", Title = "Name", ValueSelector = p => p.Name, Filterable = filterable },
            new FancyColumn<Person> { Key = "city", Title = "City", ValueSelector = p => p.City, Filterable = filterable, Hideable = hideableCity },
            new FancyColumn<Person> { Key = "id", Title = "Id", ValueSelector = p => p.Id }
        ];
    }

    private IRenderedComponent<FancyTable<Person>> RenderTable(
        List<FancyColumn<Person>>? columns = null,
        IReadOnlyList<FancyColumnFilter>? initial = null,
        Action<IReadOnlyList<FancyColumnFilter>>? onChanged = null,
        bool paginate = false,
        int currentPage = 1,
        Action<int>? onPageChanged = null,
        Func<Person, string, bool>? searchPredicate = null)
    {
        return Render<FancyTable<Person>>(parameters =>
        {
            parameters
                .Add(p => p.Items, People)
                .Add(p => p.Columns, columns ?? Columns())
                .Add(p => p.InitialColumnFilters, initial)
                .Add(p => p.ColumnFiltersChanged, EventCallback.Factory.Create<IReadOnlyList<FancyColumnFilter>>(this, f => onChanged?.Invoke(f)))
                .Add(p => p.SearchPredicate, searchPredicate);
            if (paginate)
            {
                parameters
                    .Add(p => p.PaginationEnabled, true)
                    .Add(p => p.PageSize, 10)
                    .Add(p => p.CurrentPage, currentPage)
                    .Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, page => onPageChanged?.Invoke(page)));
            }
        });
    }

    [Fact]
    public void FilterPopup_OffersOnlyTheOperatorsOfTheColumn()
    {
        List<FancyColumn<Person>> columns =
        [
            new FancyColumn<Person> { Key = "name", Title = "Name", ValueSelector = p => p.Name, Filterable = true },
            new FancyColumn<Person>
            {
                Key = "id",
                Title = "Id",
                ValueSelector = p => p.Id,
                Filterable = true,
                FilterOperators = [FancyColumnFilterOperator.Equals, FancyColumnFilterOperator.NotEquals]
            }
        ];
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(columns);

        cut.Find("th[data-column-key='id'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='name'] button.fancy-filter-button").Click();

        Assert.Equal(["Equals", "NotEquals"], cut.FindAll("th[data-column-key='id'] select option").Select(o => o.GetAttribute("value")));
        Assert.Equal(8, cut.FindAll("th[data-column-key='name'] select option").Count);
    }

    [Fact]
    public void InitialFilter_WithOperatorNotOfferedByColumn_IsIgnored()
    {
        List<FancyColumn<Person>> columns =
        [
            new FancyColumn<Person>
            {
                Key = "id",
                Title = "Id",
                ValueSelector = p => p.Id,
                Filterable = true,
                FilterOperators = [FancyColumnFilterOperator.Equals]
            }
        ];

        IRenderedComponent<FancyTable<Person>> cut = RenderTable(columns, [new FancyColumnFilter("id", FancyColumnFilterOperator.StartsWith, "1")]);

        Assert.Equal(People.Count, cut.FindAll("tbody tr").Count);
        Assert.NotNull(cut.Find("th[data-column-key='id'] i.bi-funnel"));
    }

    private static void ApplyFilter(IRenderedComponent<FancyTable<Person>> cut, string key, string operatorName, string? value)
    {
        string th = $"th[data-column-key='{key}']";
        cut.Find($"{th} button.fancy-filter-button").Click();
        cut.Find($"{th} select").Change(operatorName);
        if (value is not null)
        {
            cut.Find($"{th} input").Input(value);
        }

        cut.Find($"{th} button.btn-primary").Click();
    }

    private static int RowCount(IRenderedComponent<FancyTable<Person>> cut) => cut.FindAll("tbody tr").Count;

    [Fact]
    public void NonFilterableColumn_RendersNoFunnel()
    {
        IRenderedComponent<FancyTable<Person>> cut = RenderTable();

        Assert.Empty(cut.FindAll("th[data-column-key='id'] button.fancy-filter-button"));
        Assert.Single(cut.FindAll("th[data-column-key='name'] button.fancy-filter-button"));
        Assert.Single(cut.FindAll("th[data-column-key='city'] button.fancy-filter-button"));
    }

    [Fact]
    public void NoFilterableColumns_MarkupHasNoFilterPopup()
    {
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(Columns(filterable: false));

        Assert.Empty(cut.FindAll("button.fancy-filter-button"));
        Assert.DoesNotContain("bi-funnel", cut.Markup);
    }

    [Fact]
    public void InitialColumnFilters_AreApplied()
    {
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(
            initial: [new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")]);

        Assert.Equal(10, RowCount(cut));
        Assert.All(cut.FindAll("tbody tr"), row => Assert.Contains("Berlin", row.TextContent));
        Assert.Single(cut.FindAll("th[data-column-key='city'] i.bi-funnel-fill"));
        Assert.Empty(cut.FindAll("th[data-column-key='name'] i.bi-funnel-fill"));
    }

    [Fact]
    public void Apply_FiltersRowsAndRaisesChangedWithFullOrderedList()
    {
        IReadOnlyList<FancyColumnFilter>? received = null;
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(
            initial: [new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")],
            onChanged: f => received = f);

        ApplyFilter(cut, "name", "Contains", "a");

        Assert.Equal(
            [
                new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "a"),
                new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")
            ],
            received);
        Assert.Equal(5, RowCount(cut));
    }

    [Fact]
    public void Clear_RemovesFilterAndRaisesChanged()
    {
        IReadOnlyList<FancyColumnFilter>? received = null;
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(
            initial: [new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")],
            onChanged: f => received = f);

        cut.Find("th[data-column-key='city'] button.fancy-filter-button").Click();
        cut.Find("th[data-column-key='city'] button.btn-outline-secondary").Click();

        Assert.NotNull(received);
        Assert.Empty(received);
        Assert.Equal(30, RowCount(cut));
    }

    [Fact]
    public void FilterChange_ResetsToFirstPage()
    {
        List<int> pages = [];
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(paginate: true, currentPage: 3, onPageChanged: pages.Add);
        Assert.Contains("Bob 21", cut.Find("tbody tr").TextContent);

        ApplyFilter(cut, "name", "Contains", "anna");

        Assert.Equal([1], pages);
        Assert.Contains("Anna 2", cut.Find("tbody tr").TextContent);
        Assert.Equal(10, RowCount(cut));
        Assert.Contains("15", cut.Find(".fancy-pagination-summary").TextContent);
    }

    [Fact]
    public void FiltersCombineWithSearch()
    {
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(
            initial: [new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")]);

        cut.Find("input[placeholder]").Input("Anna");
        cut.Find("input[placeholder]").KeyUp(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(5, RowCount(cut));
    }

    [Fact]
    public void CustomSearchPredicate_NotCalledWhenOnlyFiltersActive()
    {
        int calls = 0;
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(searchPredicate: (_, _) =>
        {
            calls++;
            return true;
        });

        ApplyFilter(cut, "city", "Equals", "Berlin");

        Assert.Equal(10, RowCount(cut));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void HiddenFilteredColumn_RestoreButtonShowsFunnel()
    {
        IRenderedComponent<FancyTable<Person>> cut = RenderTable(
            Columns(hideableCity: true),
            initial: [new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")]);

        cut.Find("th[data-column-key='city'] button:has(i.bi-eye-slash)").Click();

        Assert.Single(cut.FindAll("button:has(i.bi-eye) i.bi-funnel-fill"));
        Assert.Equal(10, RowCount(cut));
    }

    private sealed record Birthday(string Name, DateOnly Born);

    [Fact]
    public void DateColumn_InitialGreaterThanFilter_ShowsOnlyLaterRows()
    {
        List<Birthday> items = [new("Old", new DateOnly(1990, 5, 1)), new("New", new DateOnly(2005, 3, 2)), new("Newer", new DateOnly(2010, 7, 3))];
        IRenderedComponent<FancyTable<Birthday>> cut = Render<FancyTable<Birthday>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.Columns,
            [
                new FancyColumn<Birthday> { Key = "name", Title = "Name", ValueSelector = b => b.Name },
                new FancyColumn<Birthday> { Key = "born", Title = "Born", ValueSelector = b => b.Born, Filterable = true, FilterType = FancyColumnFilterType.Date }
            ])
            .Add(p => p.InitialColumnFilters, [new FancyColumnFilter("born", FancyColumnFilterOperator.GreaterThan, "2000-01-01")]));

        List<string> rows = cut.FindAll("tbody tr").Select(row => row.TextContent).ToList();
        Assert.Equal(2, rows.Count);
        Assert.DoesNotContain(rows, row => row.Contains("Old"));
    }
}
