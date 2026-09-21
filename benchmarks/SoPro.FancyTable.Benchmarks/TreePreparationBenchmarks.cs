using BenchmarkDotNet.Attributes;
using SoPro.FancyTable.Components;

namespace SoPro.FancyTable.Benchmarks;

[MemoryDiagnoser]
public class TreePreparationBenchmarks
{
    private Func<List<TreeNodeState<BenchmarkRow>>> getVisible = null!;
    private Action clearCache = null!;
    private List<TreeNodeState<BenchmarkRow>> roots = null!;
    private Func<BenchmarkRow, string, bool> matches = null!;

    // Ten roots and ten children per non-leaf: 1,110 or 11,110 nodes.
    [Params(3, 4)]
    public int Depth { get; set; }

    // All nodes match "row"; only one leaf per ten siblings matches "match".
    [Params("row", "match")]
    public string Filter { get; set; } = "row";

    [GlobalSetup]
    public void Setup()
    {
        matches = static (row, filter) => row.Name.Contains(filter, StringComparison.Ordinal);
#pragma warning disable BL0005
        var table = new FancyTreeTable<BenchmarkRow>
        {
            Items = MakeTree(Depth),
            Columns = [new() { Key = "name", Title = "Name", ValueSelector = row => row.Name }],
            ChildItemsSelector = static row => row.Children,
            SearchPredicate = matches
        };
#pragma warning restore BL0005
        ComponentAccess.Bind<Action>(table, "OnParametersSet")();
        ComponentAccess.Set(table, "AppliedSearchText", Filter);
        roots = ComponentAccess.Get<List<TreeNodeState<BenchmarkRow>>>(table, "RootNodes");
        getVisible = ComponentAccess.Bind<Func<List<TreeNodeState<BenchmarkRow>>>>(table, "BuildVisibleNodes");
        clearCache = ComponentAccess.Clear(table, "preparedVisibleNodes");

        // Verify identical nodes and ancestor order, and prime the cache.
        if (!OriginalSearch().SequenceEqual(getVisible()))
            throw new InvalidOperationException("Original and current tree searches disagree.");
    }

    [Benchmark(Baseline = true)]
    public List<TreeNodeState<BenchmarkRow>> OriginalSearch()
    {
        var result = new List<TreeNodeState<BenchmarkRow>>();
        foreach (var root in roots) CollectOriginal(root, result);
        return result;
    }

    [Benchmark]
    public List<TreeNodeState<BenchmarkRow>> UncachedSearch()
    {
        clearCache();
        return getVisible();
    }

    [Benchmark]
    public List<TreeNodeState<BenchmarkRow>> CachedSearch() => getVisible();

    // Original list-per-node traversal, using the same predicate as the current
    // component. Setup checks correctness for both dense and sparse matches.
    private void CollectOriginal(TreeNodeState<BenchmarkRow> node, List<TreeNodeState<BenchmarkRow>> result)
    {
        bool include = matches(node.Item, Filter);
        int index = result.Count;
        var childMatches = new List<TreeNodeState<BenchmarkRow>>();
        foreach (var child in node.Children) CollectOriginal(child, childMatches);
        if (!include && childMatches.Count == 0) return;
        result.Insert(index, node);
        result.AddRange(childMatches);
    }

    private static List<BenchmarkRow> MakeTree(int depth)
    {
        if (depth == 0) return [];
        return Enumerable.Range(0, 10).Select(i =>
        {
            var row = new BenchmarkRow(depth == 1 && i == 0 ? "row-match" : "row");
            row.Children.AddRange(MakeTree(depth - 1));
            return row;
        }).ToList();
    }
}
