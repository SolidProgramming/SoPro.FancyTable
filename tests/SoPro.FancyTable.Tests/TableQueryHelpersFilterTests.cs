public sealed class TableQueryHelpersFilterTests
{
    [Fact]
    public void FilterOperator_HasStableExplicitValues()
    {
        Assert.Equal(0, (int)FancyColumnFilterOperator.Equals);
        Assert.Equal(1, (int)FancyColumnFilterOperator.NotEquals);
        Assert.Equal(6, (int)FancyColumnFilterOperator.IsEmpty);
        Assert.Equal(7, (int)FancyColumnFilterOperator.IsNotEmpty);
        Assert.Equal(8, (int)FancyColumnFilterOperator.GreaterThan);
        Assert.Equal(9, (int)FancyColumnFilterOperator.GreaterThanOrEqual);
        Assert.Equal(10, (int)FancyColumnFilterOperator.LessThan);
        Assert.Equal(11, (int)FancyColumnFilterOperator.LessThanOrEqual);
        Assert.Equal(12, (int)FancyColumnFilterOperator.Between);
    }

    private sealed record Person(string? Name, string? City);

    private static readonly IReadOnlyList<FancyColumn<Person>> Columns = new List<FancyColumn<Person>>
    {
        new FancyColumn<Person>
        {
            Key = "name",
            Title = "Name",
            ValueSelector = p => p.Name,
            Filterable = true
        },
        new FancyColumn<Person>
        {
            Key = "city",
            Title = "City",
            SearchTextSelector = p => p.City,
            Filterable = true
        },
        new FancyColumn<Person>
        {
            Key = "plain",
            Title = "Plain",
            ValueSelector = p => p.Name
        }
    };

    [Theory]
    [InlineData(FancyColumnFilterOperator.Equals, "  anna ", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.Equals, "ann", "Anna", false)]
    [InlineData(FancyColumnFilterOperator.NotEquals, "anna", "Anna", false)]
    [InlineData(FancyColumnFilterOperator.NotEquals, "bob", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.Contains, "NN", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.Contains, " nn", "Anna", false)] // Contains is not trimmed
    [InlineData(FancyColumnFilterOperator.NotContains, "x", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.StartsWith, "an", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.EndsWith, "NA", "Anna", true)]
    [InlineData(FancyColumnFilterOperator.IsEmpty, null, "  ", true)]
    [InlineData(FancyColumnFilterOperator.IsEmpty, null, null, true)]
    [InlineData(FancyColumnFilterOperator.IsNotEmpty, null, "Anna", true)]
    public void MatchesColumnFilters_SingleOperator(FancyColumnFilterOperator op, string? value, string? name, bool expected)
    {
        FancyColumnFilter[] filters = [new FancyColumnFilter("name", op, value)];

        bool result = TableQueryHelpers.MatchesColumnFilters(new Person(name, "Berlin"), filters, Columns);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesColumnFilters_AllFiltersMustMatch()
    {
        FancyColumnFilter[] filters =
        [
            new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "an"),
            new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "Berlin")
        ];

        Assert.True(TableQueryHelpers.MatchesColumnFilters(new Person("Anna", "Berlin"), filters, Columns));
        Assert.False(TableQueryHelpers.MatchesColumnFilters(new Person("Anna", "Bonn"), filters, Columns));
    }

    [Fact]
    public void MatchesColumnFilters_IgnoresUnknownAndNonFilterableKeys()
    {
        FancyColumnFilter[] filters =
        [
            new FancyColumnFilter("missing", FancyColumnFilterOperator.Equals, "zzz"),
            new FancyColumnFilter("plain", FancyColumnFilterOperator.Equals, "zzz")
        ];

        Assert.True(TableQueryHelpers.MatchesColumnFilters(new Person("Anna", "Berlin"), filters, Columns));
    }

    [Fact]
    public void MatchesColumnFilters_IgnoresIneffectiveFilter()
    {
        FancyColumnFilter[] filters = [new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "  ")];

        Assert.True(TableQueryHelpers.MatchesColumnFilters(new Person("Anna", "Berlin"), filters, Columns));
    }

    [Fact]
    public void MatchesColumnFilters_NoFilters_ReturnsTrue()
    {
        Assert.True(TableQueryHelpers.MatchesColumnFilters(new Person("Anna", "Berlin"), [], Columns));
    }

    [Fact]
    public void IsEffective_RequiresValueOnlyForValueOperators()
    {
        Assert.True(new FancyColumnFilter("name", FancyColumnFilterOperator.IsEmpty, null).IsEffective());
        Assert.True(new FancyColumnFilter("name", FancyColumnFilterOperator.IsNotEmpty, null).IsEffective());
        Assert.False(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, null).IsEffective());
        Assert.False(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "").IsEffective());
        Assert.False(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, " ").IsEffective());
        Assert.True(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a").IsEffective());
    }

    [Fact]
    public void ApplyColumnFilters_FiltersSequence()
    {
        Person[] people = [new Person("Anna", "Berlin"), new Person("Bob", "Bonn"), new Person("Carl", "Köln")];
        FancyColumnFilter[] filters = [new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "anna")];

        List<Person> result = TableQueryHelpers.ApplyColumnFilters(people, filters, Columns).ToList();

        Assert.Single(result);
        Assert.Equal("Anna", result[0].Name);
    }

    private sealed record Event(DateTime? At, DateOnly? Day, DateTimeOffset? Offset, int? Count, double? Ratio, string? AtText);

    private static readonly IReadOnlyList<FancyColumn<Event>> EventColumns = new List<FancyColumn<Event>>
    {
        new FancyColumn<Event> { Key = "at", Title = "At", ValueSelector = e => e.At, Filterable = true, FilterType = FancyColumnFilterType.Date },
        new FancyColumn<Event> { Key = "day", Title = "Day", ValueSelector = e => e.Day, Filterable = true, FilterType = FancyColumnFilterType.Date },
        new FancyColumn<Event> { Key = "offset", Title = "Offset", ValueSelector = e => e.Offset, Filterable = true, FilterType = FancyColumnFilterType.Date },
        new FancyColumn<Event> { Key = "count", Title = "Count", ValueSelector = e => e.Count, Filterable = true, FilterType = FancyColumnFilterType.Number },
        new FancyColumn<Event> { Key = "ratio", Title = "Ratio", ValueSelector = e => e.Ratio, Filterable = true, FilterType = FancyColumnFilterType.Number },
        new FancyColumn<Event> { Key = "attext", Title = "AtText", ValueSelector = e => e.AtText, Filterable = true, FilterType = FancyColumnFilterType.Date }
    };

    private static readonly Event SampleEvent = new(new DateTime(2026, 11, 1, 14, 30, 0), null, null, 5, null, null);

    private static bool MatchesEvent(Event item, FancyColumnFilter filter) =>
        TableQueryHelpers.MatchesColumnFilters(item, [filter], EventColumns);

    [Theory]
    [InlineData(FancyColumnFilterOperator.Equals, "2026-11-01", null, true)]
    [InlineData(FancyColumnFilterOperator.NotEquals, "2026-11-01", null, false)]
    [InlineData(FancyColumnFilterOperator.GreaterThan, "2026-11-01", null, false)]
    [InlineData(FancyColumnFilterOperator.GreaterThanOrEqual, "2026-11-01", null, true)]
    [InlineData(FancyColumnFilterOperator.LessThan, "2026-11-02", null, true)]
    [InlineData(FancyColumnFilterOperator.LessThanOrEqual, "2026-10-31", null, false)]
    [InlineData(FancyColumnFilterOperator.Between, "2026-10-01", "2026-11-01", true)]
    [InlineData(FancyColumnFilterOperator.Between, "2026-11-02", "2026-12-01", false)]
    public void DateFilter_Operators(FancyColumnFilterOperator op, string value, string? valueTo, bool expected)
    {
        Assert.Equal(expected, MatchesEvent(SampleEvent, new FancyColumnFilter("at", op, value, valueTo)));
    }

    [Fact]
    public void DateFilter_Between_SwappedBounds_Matches()
    {
        Assert.True(MatchesEvent(SampleEvent, new FancyColumnFilter("at", FancyColumnFilterOperator.Between, "2026-12-01", "2026-10-01")));
    }

    [Fact]
    public void DateFilter_DateOnlyAndOffset_UseOwnDate()
    {
        Event item = SampleEvent with
        {
            Day = new DateOnly(2026, 11, 1),
            Offset = new DateTimeOffset(2026, 11, 1, 23, 30, 0, TimeSpan.FromHours(5))
        };

        Assert.True(MatchesEvent(item, new FancyColumnFilter("day", FancyColumnFilterOperator.Equals, "2026-11-01")));
        Assert.True(MatchesEvent(item, new FancyColumnFilter("offset", FancyColumnFilterOperator.Equals, "2026-11-01")));
    }

    [Theory]
    [InlineData(FancyColumnFilterOperator.Equals, "5", null, true)]
    [InlineData(FancyColumnFilterOperator.GreaterThan, "4.5", null, true)]
    [InlineData(FancyColumnFilterOperator.LessThan, "5", null, false)]
    [InlineData(FancyColumnFilterOperator.Between, "5", "10", true)]
    [InlineData(FancyColumnFilterOperator.NotEquals, "6", null, true)]
    public void NumberFilter_Operators(FancyColumnFilterOperator op, string value, string? valueTo, bool expected)
    {
        Assert.Equal(expected, MatchesEvent(SampleEvent, new FancyColumnFilter("count", op, value, valueTo)));
    }

    [Fact]
    public void NumberFilter_DoubleComparedAsDecimal()
    {
        Assert.True(MatchesEvent(SampleEvent with { Ratio = 0.1 }, new FancyColumnFilter("ratio", FancyColumnFilterOperator.Equals, "0.1")));
    }

    [Fact]
    public void NumberFilter_NaN_IsEmpty()
    {
        Event item = SampleEvent with { Ratio = double.NaN };

        Assert.True(MatchesEvent(item, new FancyColumnFilter("ratio", FancyColumnFilterOperator.IsEmpty)));
        Assert.False(MatchesEvent(item, new FancyColumnFilter("ratio", FancyColumnFilterOperator.GreaterThan, "0")));
    }

    [Theory]
    [InlineData(1e30)]
    [InlineData((double)decimal.MaxValue)]
    public void NumberFilter_DoubleOutsideDecimalRange_IsEmpty(double ratio)
    {
        Assert.True(MatchesEvent(SampleEvent with { Ratio = ratio }, new FancyColumnFilter("ratio", FancyColumnFilterOperator.IsEmpty)));
    }

    [Fact]
    public void TypedFilter_NullValue_IsEmpty()
    {
        Event item = SampleEvent with { At = null };

        Assert.True(MatchesEvent(item, new FancyColumnFilter("at", FancyColumnFilterOperator.IsEmpty)));
        Assert.False(MatchesEvent(item, new FancyColumnFilter("at", FancyColumnFilterOperator.IsNotEmpty)));
        Assert.True(MatchesEvent(item, new FancyColumnFilter("at", FancyColumnFilterOperator.NotEquals, "2026-11-01")));
        Assert.False(MatchesEvent(item, new FancyColumnFilter("at", FancyColumnFilterOperator.GreaterThan, "2000-01-01")));
    }

    [Fact]
    public void DateFilter_StringValue_IsEmptyAndDoesNotThrow()
    {
        Event item = SampleEvent with { AtText = "2026-11-01" };

        Assert.False(MatchesEvent(item, new FancyColumnFilter("attext", FancyColumnFilterOperator.Equals, "2026-11-01")));
        Assert.True(MatchesEvent(item, new FancyColumnFilter("attext", FancyColumnFilterOperator.IsEmpty)));
    }
}
