public enum FancyColumnFilterOperator
{
    Equals = 0,
    NotEquals = 1,
    Contains = 2,
    NotContains = 3,
    StartsWith = 4,
    EndsWith = 5,
    IsEmpty = 6,
    IsNotEmpty = 7,
    GreaterThan = 8,
    GreaterThanOrEqual = 9,
    LessThan = 10,
    LessThanOrEqual = 11,
    Between = 12
}

// The kind of values a filterable column holds; selects the filter input and the offered operators.
public enum FancyColumnFilterType
{
    Text = 0,
    Date = 1,
    Number = 2
}

// ValueTo is the upper bound of Between and null for every other operator.
public sealed record FancyColumnFilter(string Key, FancyColumnFilterOperator Operator, string? Value = null, string? ValueTo = null);

internal static class FancyColumnFilterExtensions
{
    private static readonly IReadOnlyList<FancyColumnFilterOperator> TextOperators =
    [
        FancyColumnFilterOperator.Equals,
        FancyColumnFilterOperator.NotEquals,
        FancyColumnFilterOperator.Contains,
        FancyColumnFilterOperator.NotContains,
        FancyColumnFilterOperator.StartsWith,
        FancyColumnFilterOperator.EndsWith,
        FancyColumnFilterOperator.IsEmpty,
        FancyColumnFilterOperator.IsNotEmpty
    ];

    private static readonly IReadOnlyList<FancyColumnFilterOperator> ComparisonOperators =
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

    // All operators a column of the given type can offer, in display order.
    internal static IReadOnlyList<FancyColumnFilterOperator> OperatorsFor(FancyColumnFilterType type) =>
        type is FancyColumnFilterType.Date or FancyColumnFilterType.Number ? ComparisonOperators : TextOperators;

    // The operators a column offers: the given ones valid for its type, without duplicates, or all of the type's operators if none remain.
    internal static IReadOnlyList<FancyColumnFilterOperator> ResolveOperators(
        IReadOnlyList<FancyColumnFilterOperator>? operators,
        FancyColumnFilterType type)
    {
        IReadOnlyList<FancyColumnFilterOperator> valid = OperatorsFor(type);
        List<FancyColumnFilterOperator> resolved = operators?.Where(valid.Contains).Distinct().ToList() ?? [];
        return resolved.Count == 0 ? valid : resolved;
    }

    // The operator preselected for a new filter: Contains if offered, otherwise the first offered operator.
    internal static FancyColumnFilterOperator DefaultOperator(this IReadOnlyList<FancyColumnFilterOperator> operators)
    {
        return operators.Contains(FancyColumnFilterOperator.Contains) ? FancyColumnFilterOperator.Contains : operators[0];
    }

    internal static bool RequiresValue(this FancyColumnFilterOperator op)
    {
        return op is not (FancyColumnFilterOperator.IsEmpty or FancyColumnFilterOperator.IsNotEmpty);
    }

    internal static bool RequiresSecondValue(this FancyColumnFilterOperator op)
    {
        return op is FancyColumnFilterOperator.Between;
    }

    internal static bool IsEffective(this FancyColumnFilter filter)
    {
        if (!filter.Operator.RequiresValue())
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(filter.Value)
            && (!filter.Operator.RequiresSecondValue() || !string.IsNullOrWhiteSpace(filter.ValueTo));
    }
}
