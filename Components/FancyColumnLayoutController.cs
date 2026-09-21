internal sealed class FancyColumnLayoutController<TItem>
{
    private readonly List<string> order = [];
    private readonly Dictionary<string, double> widths = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, FancyColumn<TItem>> columns = new Dictionary<string, FancyColumn<TItem>>();
    private bool initialized;

    public IReadOnlyDictionary<string, double> Widths => widths;

    public void Synchronize(
        IReadOnlyList<FancyColumn<TItem>> sourceColumns,
        IReadOnlyList<FancyColumnLayout>? initialLayout,
        string? pinnedColumnKey)
    {
        columns = sourceColumns
            .GroupBy(column => column.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        if (!initialized)
        {
            HashSet<string> addedKeys = new(StringComparer.Ordinal);
            foreach (FancyColumnLayout item in initialLayout ?? [])
            {
                if (columns.ContainsKey(item.Key) && addedKeys.Add(item.Key))
                {
                    order.Add(item.Key);
                    if (item.Width is double width && double.IsFinite(width))
                    {
                        widths[item.Key] = ClampWidth(item.Key, width);
                    }
                }
            }

            foreach (FancyColumn<TItem> column in sourceColumns)
            {
                if (addedKeys.Add(column.Key))
                {
                    order.Add(column.Key);
                }

                if (!widths.ContainsKey(column.Key) && column.Width is double width && double.IsFinite(width))
                {
                    widths[column.Key] = ClampWidth(column.Key, width);
                }
            }

            initialized = true;
        }
        else
        {
            order.RemoveAll(key => !columns.ContainsKey(key));
            widths.Keys.Where(key => !columns.ContainsKey(key)).ToList().ForEach(key => widths.Remove(key));

            foreach (FancyColumn<TItem> column in sourceColumns)
            {
                if (!order.Contains(column.Key, StringComparer.Ordinal))
                {
                    order.Add(column.Key);
                    if (column.Width is double width && double.IsFinite(width))
                    {
                        widths[column.Key] = ClampWidth(column.Key, width);
                    }
                }
            }
        }

        if (pinnedColumnKey is not null && order.Remove(pinnedColumnKey))
        {
            order.Insert(0, pinnedColumnKey);
        }

        foreach (string key in widths.Keys.ToList())
        {
            widths[key] = ClampWidth(key, widths[key]);
        }
    }

    public IReadOnlyList<FancyColumn<TItem>> GetOrderedColumns() =>
        order.Where(columns.ContainsKey).Select(key => columns[key]).ToList();

    public bool IsConstrained(string key) =>
        widths.ContainsKey(key) || (columns.TryGetValue(key, out FancyColumn<TItem>? column) && column.MaxWidth.HasValue);

    public string? GetColumnStyle(string key)
    {
        if (widths.TryGetValue(key, out double width))
        {
            return FormattableString.Invariant($"width:{width:0.##}px;min-width:{width:0.##}px;max-width:{width:0.##}px;");
        }

        return columns.TryGetValue(key, out FancyColumn<TItem>? column) && column.MaxWidth is double maxWidth
            ? FormattableString.Invariant($"max-width:{maxWidth:0.##}px;")
            : null;
    }

    public void ApplyMeasuredWidths(IEnumerable<FancyColumnLayout> measurements)
    {
        foreach (FancyColumnLayout measurement in measurements)
        {
            if (columns.ContainsKey(measurement.Key)
                && measurement.Width is double width
                && double.IsFinite(width))
            {
                widths[measurement.Key] = ClampWidth(measurement.Key, width);
            }
        }
    }

    public bool TryReorder(string sourceKey, string targetKey, bool insertAfter, string? pinnedColumnKey)
    {
        if (sourceKey == targetKey
            || !columns.TryGetValue(sourceKey, out FancyColumn<TItem>? source)
            || !columns.TryGetValue(targetKey, out FancyColumn<TItem>? target)
            || !source.Reorderable
            || !target.Reorderable
            || sourceKey == pinnedColumnKey
            || targetKey == pinnedColumnKey)
        {
            return false;
        }

        int sourceIndex = order.IndexOf(sourceKey);
        int targetIndex = order.IndexOf(targetKey);
        int lower = Math.Min(sourceIndex, targetIndex);
        int upper = Math.Max(sourceIndex, targetIndex);
        if (sourceIndex < 0 || targetIndex < 0
            || order.Skip(lower + 1).Take(upper - lower - 1).Any(key => !columns[key].Reorderable || key == pinnedColumnKey))
        {
            return false;
        }

        order.RemoveAt(sourceIndex);
        targetIndex = order.IndexOf(targetKey);
        order.Insert(targetIndex + (insertAfter ? 1 : 0), sourceKey);
        return true;
    }

    public IReadOnlyList<FancyColumnLayout> GetLayout() =>
        order.Where(columns.ContainsKey)
            .Select(key => new FancyColumnLayout(key, widths.TryGetValue(key, out double width) ? width : null))
            .ToList();

    private double ClampWidth(string key, double width)
    {
        FancyColumn<TItem> column = columns[key];
        double minimum = Math.Max(24, column.MinWidth ?? 24);
        double maximum = Math.Max(minimum, column.MaxWidth ?? double.MaxValue);
        return Math.Clamp(width, minimum, maximum);
    }
}
