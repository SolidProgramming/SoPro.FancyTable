public enum FancyColumnFilterOperator
{
    Equals = 0,
    NotEquals = 1,
    Contains = 2,
    NotContains = 3,
    StartsWith = 4,
    EndsWith = 5,
    IsEmpty = 6,
    IsNotEmpty = 7
}

public sealed record FancyColumnFilter(string Key, FancyColumnFilterOperator Operator, string? Value = null);

internal static class FancyColumnFilterExtensions
{
    internal static readonly IReadOnlyList<FancyColumnFilterOperator> AllOperators = Enum.GetValues<FancyColumnFilterOperator>();

    // The operators a column offers: the defined ones of the given list without duplicates, or all operators if none remain.
    internal static IReadOnlyList<FancyColumnFilterOperator> ResolveOperators(IReadOnlyList<FancyColumnFilterOperator>? operators)
    {
        List<FancyColumnFilterOperator> resolved = operators?.Where(Enum.IsDefined).Distinct().ToList() ?? [];
        return resolved.Count == 0 ? AllOperators : resolved;
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

    internal static bool IsEffective(this FancyColumnFilter filter)
    {
        return !filter.Operator.RequiresValue() || !string.IsNullOrWhiteSpace(filter.Value);
    }
}
