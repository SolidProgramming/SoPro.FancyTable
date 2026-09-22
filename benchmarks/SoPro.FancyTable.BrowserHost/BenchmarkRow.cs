namespace SoPro.FancyTable.BrowserHost;

public sealed class BenchmarkRow(string id, int number)
{
    public string Id { get; } = id;
    public string Name { get; } = $"Item {number:D5}";
    public string Category { get; } = number % 10 == 0 ? "needle" : "other";
    public int Quantity { get; } = number % 100;
    public string Status { get; } = number % 2 == 0 ? "Active" : "Inactive";
    public List<BenchmarkRow> Children { get; } = [];
    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?> { ["data-row-id"] = id };

    public static List<BenchmarkRow> Flat(int count) => Enumerable.Range(0, count).Reverse()
        .Select(i => new BenchmarkRow($"f-{i:D5}", i)).ToList();

    public static List<BenchmarkRow> Tree(int levels, string prefix = "t") => Enumerable.Range(0, 10)
        .Select(i =>
        {
            var row = new BenchmarkRow($"{prefix}-{i}", i);
            if (levels > 1) row.Children.AddRange(Tree(levels - 1, row.Id));
            return row;
        }).ToList();
}
