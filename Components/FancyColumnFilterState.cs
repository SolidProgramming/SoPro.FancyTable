internal sealed class FancyColumnFilterState<TItem>
{
    private readonly Dictionary<string, FancyColumnFilter> filters = new(StringComparer.Ordinal);
    private List<string> columnOrder = [];
    private Dictionary<string, IReadOnlyList<FancyColumnFilterOperator>> operatorsByFilterableKey = new(StringComparer.Ordinal);
    private bool initialApplied;

    public bool HasActiveFilters => filters.Count > 0;

    public void Synchronize(
        IReadOnlyList<FancyColumn<TItem>> columns,
        IReadOnlyList<FancyColumnFilter>? initialFilters)
    {
        columnOrder = columns.Select(column => column.Key).Distinct(StringComparer.Ordinal).ToList();
        operatorsByFilterableKey = new(StringComparer.Ordinal);
        foreach (FancyColumn<TItem> column in columns.Where(column => column.Filterable))
        {
            operatorsByFilterableKey.TryAdd(column.Key, FancyColumnFilterExtensions.ResolveOperators(column.FilterOperators, FancyColumnFilterType.Text));
        }

        if (!initialApplied)
        {
            foreach (FancyColumnFilter filter in initialFilters ?? [])
            {
                FancyColumnFilter normalized = Normalize(filter);
                if (normalized.IsEffective()
                    && IsAllowed(normalized)
                    && !filters.ContainsKey(normalized.Key))
                {
                    filters[normalized.Key] = normalized;
                }
            }

            initialApplied = true;
        }

        foreach (string key in filters.Where(entry => !IsAllowed(entry.Value)).Select(entry => entry.Key).ToList())
        {
            filters.Remove(key);
        }
    }

    public FancyColumnFilter? Get(string key) =>
        filters.TryGetValue(key, out FancyColumnFilter? filter) ? filter : null;

    public bool Set(FancyColumnFilter filter)
    {
        FancyColumnFilter normalized = Normalize(filter);
        if (!normalized.IsEffective()
            || !IsAllowed(normalized)
            || (filters.TryGetValue(normalized.Key, out FancyColumnFilter? existing) && existing == normalized))
        {
            return false;
        }

        filters[normalized.Key] = normalized;
        return true;
    }

    public bool Remove(string key) => filters.Remove(key);

    public IReadOnlyList<FancyColumnFilter> GetFilters() =>
        columnOrder.Where(filters.ContainsKey).Select(key => filters[key]).ToList();

    // A filter is allowed if its column is filterable and offers its operator.
    private bool IsAllowed(FancyColumnFilter filter) =>
        operatorsByFilterableKey.TryGetValue(filter.Key, out IReadOnlyList<FancyColumnFilterOperator>? operators)
        && operators.Contains(filter.Operator);

    private static FancyColumnFilter Normalize(FancyColumnFilter filter) =>
        filter.Operator.RequiresValue() || filter.Value is null ? filter : filter with { Value = null };
}
