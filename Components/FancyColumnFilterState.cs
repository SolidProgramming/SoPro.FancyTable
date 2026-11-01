internal sealed class FancyColumnFilterState<TItem>
{
    private readonly Dictionary<string, FancyColumnFilter> filters = new(StringComparer.Ordinal);
    private List<string> columnOrder = [];
    private HashSet<string> filterableKeys = new(StringComparer.Ordinal);
    private bool initialApplied;

    public bool HasActiveFilters => filters.Count > 0;

    public void Synchronize(
        IReadOnlyList<FancyColumn<TItem>> columns,
        IReadOnlyList<FancyColumnFilter>? initialFilters)
    {
        columnOrder = columns.Select(column => column.Key).Distinct(StringComparer.Ordinal).ToList();
        filterableKeys = columns
            .Where(column => column.Filterable)
            .Select(column => column.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (!initialApplied)
        {
            foreach (FancyColumnFilter filter in initialFilters ?? [])
            {
                FancyColumnFilter normalized = Normalize(filter);
                if (normalized.IsEffective()
                    && filterableKeys.Contains(normalized.Key)
                    && !filters.ContainsKey(normalized.Key))
                {
                    filters[normalized.Key] = normalized;
                }
            }

            initialApplied = true;
        }

        foreach (string key in filters.Keys.Where(key => !filterableKeys.Contains(key)).ToList())
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
            || !filterableKeys.Contains(normalized.Key)
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

    private static FancyColumnFilter Normalize(FancyColumnFilter filter) =>
        filter.Operator.RequiresValue() || filter.Value is null ? filter : filter with { Value = null };
}
