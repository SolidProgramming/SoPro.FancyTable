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
            .Select(c => GetColumnText(c, item))
            .Any(v => !string.IsNullOrWhiteSpace(v) && v.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    internal static string GetColumnText<TItem>(FancyColumn<TItem> column, TItem item)
    {
        return column.SearchTextSelector?.Invoke(item) ?? column.ValueSelector?.Invoke(item)?.ToString() ?? string.Empty;
    }

    internal static IEnumerable<TItem> ApplyColumnFilters<TItem>(
        IEnumerable<TItem> items,
        IReadOnlyCollection<FancyColumnFilter> filters,
        IReadOnlyList<FancyColumn<TItem>> columns)
    {
        if (filters.Count == 0)
            return items;

        return items.Where(item => MatchesColumnFilters(item, filters, columns));
    }

    internal static bool MatchesColumnFilters<TItem>(
        TItem item,
        IReadOnlyCollection<FancyColumnFilter> filters,
        IReadOnlyList<FancyColumn<TItem>> columns)
    {
        foreach (FancyColumnFilter filter in filters)
        {
            if (!filter.IsEffective())
                continue;

            FancyColumn<TItem>? column = columns.FirstOrDefault(c => c.Key == filter.Key);
            if (column is null || !column.Filterable)
                continue;

            bool matches = column.FilterType == FancyColumnFilterType.Text
                ? MatchesFilter(GetColumnText(column, item), filter)
                : FancyFilterValues.Matches(column.FilterType, column.ValueSelector?.Invoke(item), filter);

            if (!matches)
                return false;
        }

        return true;
    }

    private static bool MatchesFilter(string text, FancyColumnFilter filter)
    {
        string value = filter.Value ?? string.Empty;

        return filter.Operator switch
        {
            FancyColumnFilterOperator.Equals => string.Equals(text.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.NotEquals => !string.Equals(text.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.Contains => text.Contains(value, StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.NotContains => !text.Contains(value, StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.StartsWith => text.StartsWith(value, StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.EndsWith => text.EndsWith(value, StringComparison.OrdinalIgnoreCase),
            FancyColumnFilterOperator.IsEmpty => string.IsNullOrWhiteSpace(text),
            FancyColumnFilterOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
    }
}
