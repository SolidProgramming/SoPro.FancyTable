public sealed class FancyColumnFilterStateTests
{
    private sealed record Person(string Name);

    private static FancyColumn<Person> Column(string key, bool filterable = true)
    {
        return new FancyColumn<Person>
        {
            Key = key,
            Title = key,
            ValueSelector = p => p.Name,
            Filterable = filterable
        };
    }

    private static List<FancyColumn<Person>> DefaultColumns()
    {
        return [Column("name"), Column("name2"), Column("city", false), Column("zip")];
    }

    [Fact]
    public void Synchronize_AppliesInitialFiltersOnce()
    {
        FancyColumnFilterState<Person> state = new();
        List<FancyColumn<Person>> columns = DefaultColumns();

        state.Synchronize(columns, [new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a")]);
        state.Synchronize(columns, [new FancyColumnFilter("zip", FancyColumnFilterOperator.Equals, "b")]);

        Assert.Equal([new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a")], state.GetFilters());
    }

    [Fact]
    public void Synchronize_DropsUnknownNonFilterableDuplicateAndIneffective()
    {
        FancyColumnFilterState<Person> state = new();

        state.Synchronize(DefaultColumns(),
        [
            new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a"),
            new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "b"),
            new FancyColumnFilter("missing", FancyColumnFilterOperator.Equals, "x"),
            new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "y"),
            new FancyColumnFilter("name2", FancyColumnFilterOperator.Contains, "  ")
        ]);

        Assert.Equal([new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a")], state.GetFilters());
        Assert.True(state.HasActiveFilters);
    }

    [Fact]
    public void Synchronize_DropsFilterWhenColumnRemovedOrNoLongerFilterable()
    {
        FancyColumnFilterState<Person> state = new();
        state.Synchronize(DefaultColumns(),
        [
            new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a"),
            new FancyColumnFilter("zip", FancyColumnFilterOperator.Equals, "b")
        ]);

        state.Synchronize([Column("name", false)], null);

        Assert.Empty(state.GetFilters());
        Assert.False(state.HasActiveFilters);
    }

    [Fact]
    public void Set_NormalizesEmptyOperatorValueToNull()
    {
        FancyColumnFilterState<Person> state = new();
        state.Synchronize(DefaultColumns(), null);

        Assert.True(state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.IsEmpty, "x")));

        Assert.Null(state.Get("name")!.Value);
    }

    [Fact]
    public void Set_ReturnsFalseForUnchangedOrIneffective()
    {
        FancyColumnFilterState<Person> state = new();
        state.Synchronize(DefaultColumns(), null);
        FancyColumnFilter filter = new("name", FancyColumnFilterOperator.Equals, "a");

        Assert.True(state.Set(filter));
        Assert.False(state.Set(filter));
        Assert.True(state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.IsEmpty, "x")));
        Assert.False(state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.IsEmpty, null)));
        Assert.False(state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, " ")));
        Assert.False(state.Set(new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "a")));
        Assert.False(state.Set(new FancyColumnFilter("missing", FancyColumnFilterOperator.Equals, "a")));
        Assert.Equal(FancyColumnFilterOperator.IsEmpty, state.Get("name")!.Operator);
        Assert.Null(state.Get("city"));
    }

    [Fact]
    public void Remove_ReturnsFalseWhenMissing()
    {
        FancyColumnFilterState<Person> state = new();
        state.Synchronize(DefaultColumns(), null);
        state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "a"));

        Assert.False(state.Remove("zip"));
        Assert.True(state.Remove("name"));
        Assert.False(state.Remove("name"));
    }

    [Fact]
    public void GetFilters_FollowsColumnDefinitionOrder()
    {
        FancyColumnFilterState<Person> state = new();
        state.Synchronize([Column("name"), Column("city")], null);

        state.Set(new FancyColumnFilter("city", FancyColumnFilterOperator.Equals, "c"));
        state.Set(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "n"));

        Assert.Equal(["name", "city"], state.GetFilters().Select(f => f.Key));
    }
}
