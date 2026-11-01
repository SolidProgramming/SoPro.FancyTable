public enum FancyColumnFilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    IsEmpty,
    IsNotEmpty
}

public sealed record FancyColumnFilter(string Key, FancyColumnFilterOperator Operator, string? Value = null);

internal static class FancyColumnFilterExtensions
{
    internal static bool RequiresValue(this FancyColumnFilterOperator op)
    {
        return op is not (FancyColumnFilterOperator.IsEmpty or FancyColumnFilterOperator.IsNotEmpty);
    }

    internal static bool IsEffective(this FancyColumnFilter filter)
    {
        return !filter.Operator.RequiresValue() || !string.IsNullOrWhiteSpace(filter.Value);
    }
}
