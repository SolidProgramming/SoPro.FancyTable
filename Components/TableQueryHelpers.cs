internal static class TableQueryHelpers
{
    internal static IEnumerable<TItem> ApplySearch<TItem>(
        IEnumerable<TItem> items,
        string filter,
        IReadOnlyList<FancyColumn<TItem>> columns,
        Func<TItem, string, bool>? searchPredicate)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return items;

        return items.Where(item => MatchesSearch(item, filter, columns, searchPredicate));
    }

    internal static IEnumerable<TItem> ApplySort<TItem>(
        IEnumerable<TItem> items,
        string? sortColumnKey,
        bool sortAscending,
        IReadOnlyList<FancyColumn<TItem>> columns)
    {
        if (string.IsNullOrWhiteSpace(sortColumnKey))
            return items;

        FancyColumn<TItem>? column = columns.FirstOrDefault(c => c.Key == sortColumnKey);
        if (column is null)
            return items;

        Func<TItem, IComparable?> selector = column.SortValueSelector
            ?? (item => column.ValueSelector?.Invoke(item) as IComparable);

        return sortAscending
            ? items.OrderBy(selector)
            : items.OrderByDescending(selector);
    }

    internal static bool MatchesSearch<TItem>(
        TItem item,
        string filter,
        IReadOnlyList<FancyColumn<TItem>> columns,
        Func<TItem, string, bool>? searchPredicate)
    {
        if (searchPredicate is not null)
            return searchPredicate(item, filter);

        return columns
            .Where(c => c.Searchable)
            .Select(c => c.SearchTextSelector?.Invoke(item) ?? c.ValueSelector?.Invoke(item)?.ToString())
            .Any(v => !string.IsNullOrWhiteSpace(v) && v.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
