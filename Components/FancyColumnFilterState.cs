internal sealed class FancyColumnFilterState<TItem>
{
    private readonly Dictionary<string, FancyColumnFilter> filters = new(StringComparer.Ordinal);
    private List<string> columnOrder = [];
    private Dictionary<string, FilterableColumn> filterableColumnsByKey = new(StringComparer.Ordinal);
    private bool initialApplied;

    public bool HasActiveFilters => filters.Count > 0;

    public void Synchronize(
        IReadOnlyList<FancyColumn<TItem>> columns,
        IReadOnlyList<FancyColumnFilter>? initialFilters)
    {
        columnOrder = columns.Select(column => column.Key).Distinct(StringComparer.Ordinal).ToList();
        filterableColumnsByKey = new(StringComparer.Ordinal);
        foreach (FancyColumn<TItem> column in columns.Where(column => column.Filterable))
        {
            filterableColumnsByKey.TryAdd(
                column.Key,
                new FilterableColumn(column.FilterType, FancyColumnFilterExtensions.ResolveOperators(column.FilterOperators, column.FilterType)));
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

    // A filter is allowed if its column is filterable, offers its operator and can read its values.
    private bool IsAllowed(FancyColumnFilter filter) =>
        filterableColumnsByKey.TryGetValue(filter.Key, out FilterableColumn? column)
        && column.Operators.Contains(filter.Operator)
        && (!filter.Operator.RequiresValue() || FancyFilterValues.IsValidValue(column.Type, filter.Value))
        && (!filter.Operator.RequiresSecondValue() || FancyFilterValues.IsValidValue(column.Type, filter.ValueTo));

    // Drops the values the operator does not use: Value for IsEmpty/IsNotEmpty, ValueTo for everything but Between.
    private static FancyColumnFilter Normalize(FancyColumnFilter filter) => filter with
    {
        Value = filter.Operator.RequiresValue() ? filter.Value : null,
        ValueTo = filter.Operator.RequiresSecondValue() ? filter.ValueTo : null
    };

    private sealed record FilterableColumn(FancyColumnFilterType Type, IReadOnlyList<FancyColumnFilterOperator> Operators);
}
