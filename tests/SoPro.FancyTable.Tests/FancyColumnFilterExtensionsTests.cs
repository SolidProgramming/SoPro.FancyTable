using System.Text.Json;

public sealed class FancyColumnFilterExtensionsTests
{
    private static readonly FancyColumnFilterOperator[] ComparisonOperators =
    [
        FancyColumnFilterOperator.Equals,
        FancyColumnFilterOperator.NotEquals,
        FancyColumnFilterOperator.GreaterThan,
        FancyColumnFilterOperator.GreaterThanOrEqual,
        FancyColumnFilterOperator.LessThan,
        FancyColumnFilterOperator.LessThanOrEqual,
        FancyColumnFilterOperator.Between,
        FancyColumnFilterOperator.IsEmpty,
        FancyColumnFilterOperator.IsNotEmpty
    ];

    [Fact]
    public void OperatorsFor_Text_IsTheEightTextOperators()
    {
        Assert.Equal(
            [
                FancyColumnFilterOperator.Equals,
                FancyColumnFilterOperator.NotEquals,
                FancyColumnFilterOperator.Contains,
                FancyColumnFilterOperator.NotContains,
                FancyColumnFilterOperator.StartsWith,
                FancyColumnFilterOperator.EndsWith,
                FancyColumnFilterOperator.IsEmpty,
                FancyColumnFilterOperator.IsNotEmpty
            ],
            FancyColumnFilterExtensions.OperatorsFor(FancyColumnFilterType.Text));
    }

    [Theory]
    [InlineData(FancyColumnFilterType.Date)]
    [InlineData(FancyColumnFilterType.Number)]
    public void OperatorsFor_DateAndNumber_AreComparisonOperators(FancyColumnFilterType type)
    {
        Assert.Equal(ComparisonOperators, FancyColumnFilterExtensions.OperatorsFor(type));
    }

    [Fact]
    public void ResolveOperators_DropsOperatorsNotValidForType()
    {
        IReadOnlyList<FancyColumnFilterOperator> resolved = FancyColumnFilterExtensions.ResolveOperators(
            [FancyColumnFilterOperator.Contains, FancyColumnFilterOperator.GreaterThan, FancyColumnFilterOperator.Equals],
            FancyColumnFilterType.Date);

        Assert.Equal([FancyColumnFilterOperator.GreaterThan, FancyColumnFilterOperator.Equals], resolved);
    }

    [Fact]
    public void ResolveOperators_NoneValid_FallsBackToTypeSet()
    {
        IReadOnlyList<FancyColumnFilterOperator> resolved = FancyColumnFilterExtensions.ResolveOperators(
            [FancyColumnFilterOperator.Contains],
            FancyColumnFilterType.Number);

        Assert.Equal(ComparisonOperators, resolved);
    }

    [Fact]
    public void DefaultOperator_DateSet_IsEquals()
    {
        Assert.Equal(
            FancyColumnFilterOperator.Equals,
            FancyColumnFilterExtensions.OperatorsFor(FancyColumnFilterType.Date).DefaultOperator());
    }

    [Fact]
    public void IsEffective_Between_RequiresBothValues()
    {
        Assert.False(new FancyColumnFilter("d", FancyColumnFilterOperator.Between, "2026-01-01").IsEffective());
        Assert.False(new FancyColumnFilter("d", FancyColumnFilterOperator.Between, "2026-01-01", " ").IsEffective());
        Assert.True(new FancyColumnFilter("d", FancyColumnFilterOperator.Between, "2026-01-01", "2026-02-01").IsEffective());
    }

    [Fact]
    public void Deserialize_JsonWithoutValueTo_HasNullValueTo()
    {
        FancyColumnFilter? filter = JsonSerializer.Deserialize<FancyColumnFilter>("{\"Key\":\"name\",\"Operator\":2,\"Value\":\"x\"}");

        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "x"), filter);
    }
}
