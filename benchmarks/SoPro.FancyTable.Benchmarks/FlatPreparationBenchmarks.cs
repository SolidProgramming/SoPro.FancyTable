using BenchmarkDotNet.Attributes;
using SoPro.FancyTable.Components;

namespace SoPro.FancyTable.Benchmarks;

[MemoryDiagnoser]
public class FlatPreparationBenchmarks
{
    private Func<object> getPage = null!;
    private Action clearCache = null!;

    [Params(1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Deliberately exercise preparation without a renderer or DOM. Parameters
        // and lifecycle initialization are supplied explicitly in this harness.
#pragma warning disable BL0005
        var table = new FancyTable<BenchmarkRow>
        {
            Items = Enumerable.Range(0, RowCount).Reverse()
                .Select(i => new BenchmarkRow($"row{i:D6}")).ToList(),
            Columns = [new()
            {
                Key = "name", Title = "Name", Sortable = true,
                ValueSelector = row => row.Name, SortValueSelector = row => row.Name
            }],
            SearchPredicate = static (row, filter) => row.Name.Contains(filter, StringComparison.Ordinal),
            PaginationEnabled = true,
            PageSize = 25
        };
#pragma warning restore BL0005
        ComponentAccess.Bind<Action>(table, "OnParametersSet")();
        ComponentAccess.Set(table, "AppliedSearchText", "row");
        ComponentAccess.Set(table, "SortColumnKey", "name");
        ComponentAccess.Set(table, "SortAscending", true);
        getPage = ComponentAccess.Bind<Func<object>>(table, "GetPagedItems");
        clearCache = ComponentAccess.Clear(table, "preparedItems");
        getPage(); // Warm the cache outside the measured operation.

        var prepared = ComponentAccess.Get<IReadOnlyList<BenchmarkRow>>(table, "preparedItems");
        if (prepared.Count != RowCount || prepared[0].Name != "row000000")
            throw new InvalidOperationException("Unexpected prepared flat-table result.");
    }

    // Full search/sort/materialization on every invocation, as before caching.
    // Includes the cache reset; does not claim to measure an older binary.
    [Benchmark(Baseline = true)]
    public object UncachedQuery()
    {
        clearCache();
        return getPage();
    }

    [Benchmark]
    public object CachedQuery() => getPage();
}
