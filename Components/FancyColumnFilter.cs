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
    internal static bool RequiresValue(this FancyColumnFilterOperator op)
    {
        return op is not (FancyColumnFilterOperator.IsEmpty or FancyColumnFilterOperator.IsNotEmpty);
    }

    internal static bool IsEffective(this FancyColumnFilter filter)
    {
        return !filter.Operator.RequiresValue() || !string.IsNullOrWhiteSpace(filter.Value);
    }
}
