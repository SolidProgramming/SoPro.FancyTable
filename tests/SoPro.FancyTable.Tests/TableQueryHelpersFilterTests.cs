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
}
