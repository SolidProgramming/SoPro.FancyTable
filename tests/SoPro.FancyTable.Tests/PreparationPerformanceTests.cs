using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;
using Xunit.Abstractions;
using Row = SoPro.FancyTable.Tests.PreparationTests.Row;

namespace SoPro.FancyTable.Tests;

// A focused preparation benchmark, not a browser/rendering benchmark. Reflection
// keeps measurement hooks out of the library's public API.
public sealed class PreparationPerformanceTests(ITestOutputHelper output) : BunitContext
{
    private const int Iterations = 100;
    private int searches;
    private int sorts;

    [Fact]
    [Trait("Category", "Performance")]
    public async Task Report_preparation_allocations_and_selector_calls()
    {
        var items = Enumerable.Range(0, 10_000).Select(i => new Row($"row{i:D5}")).ToList();
        List<FancyColumn<Row>> columns = [new()
        {
            Key = "name", Title = "Name", Sortable = true, ValueSelector = r => r.Name,
            SortValueSelector = r => { sorts++; return r.Name; }
        }];
        bool Search(Row row, string filter) { searches++; return row.Name.Contains(filter); }
        var flat = Render<FancyTable<Row>>(p => p.Add(c => c.Items, items)
            .Add(c => c.Columns, columns).Add(c => c.SearchPredicate, Search)
            .Add(c => c.PaginationEnabled, true).Add(c => c.PageSize, 25));
        flat.Find("input").Input("row");
        flat.Find("input").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        flat.Find("th button").Click();

        var getPage = Method(flat.Instance, "GetPagedItems").CreateDelegate<Func<object>>(flat.Instance);
        var cache = Field(flat.Instance, "preparedItems");
        await flat.InvokeAsync(() =>
        {
            Measure("Flat, 10,000 rows, uncached query", () =>
            {
                cache.SetValue(flat.Instance, null);
                return getPage();
            }, expectedSearches: 10_000 * Iterations, expectedSorts: 10_000 * Iterations);
            Measure("Flat, 10,000 rows, cached query", getPage, 0, 0);
        });

        var roots = MakeTree(depth: 4);
        var tree = Render<FancyTreeTable<Row>>(p => p.Add(c => c.Items, roots)
            .Add(c => c.Columns, columns).Add(c => c.ChildItemsSelector, r => r.Children)
            .Add(c => c.SearchPredicate, Search).Add(c => c.PaginationEnabled, true)
            .Add(c => c.PageSize, 25));
        tree.Find("input").Input("row");
        tree.Find("input").KeyUp(new KeyboardEventArgs { Key = "Enter" });
        var getVisible = Method(tree.Instance, "BuildVisibleNodes")
            .CreateDelegate<Func<List<TreeNodeState<Row>>>>(tree.Instance);
        var treeCache = Field(tree.Instance, "preparedVisibleNodes");
        var nodes = (List<TreeNodeState<Row>>)Field(tree.Instance, "RootNodes").GetValue(tree.Instance)!;

        await tree.InvokeAsync(() =>
        {
            // Reference algorithm from before this change: a temporary list for
            // each node. Check the output before comparing its allocations.
            Assert.Equal(LegacySearch(nodes), getVisible());
            Measure("Tree, 11,110 nodes, original search", () => LegacySearch(nodes), 11_110 * Iterations, 0);
            Measure("Tree, 11,110 nodes, new search (cold)", () =>
            {
                treeCache.SetValue(tree.Instance, null);
                return getVisible();
            }, 11_110 * Iterations, 0);
            Measure("Tree, 11,110 nodes, cached search", () => getVisible(), 0, 0);
        });
    }

    private void Measure(string label, Func<object> operation, int expectedSearches, int expectedSorts)
    {
        for (int i = 0; i < 5; i++) operation();
        searches = sorts = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++) GC.KeepAlive(operation());
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"{label}: {allocated:N0} bytes / {Iterations} calls; search={searches:N0}, sort={sorts:N0}");
        Assert.Equal(expectedSearches, searches);
        Assert.Equal(expectedSorts, sorts);
    }

    private List<TreeNodeState<Row>> LegacySearch(List<TreeNodeState<Row>> roots)
    {
        var result = new List<TreeNodeState<Row>>();
        foreach (var root in roots) Collect(root, result);
        return result;
    }

    private void Collect(TreeNodeState<Row> node, List<TreeNodeState<Row>> result)
    {
        searches++;
        bool matches = node.Item.Name.Contains("row");
        int index = result.Count;
        var childMatches = new List<TreeNodeState<Row>>();
        foreach (var child in node.Children) Collect(child, childMatches);
        if (!matches && childMatches.Count == 0) return;
        result.Insert(index, node);
        result.AddRange(childMatches);
    }

    private static List<Row> MakeTree(int depth)
    {
        if (depth == 0) return [];
        return Enumerable.Range(0, 10).Select(i =>
        {
            var row = new Row($"row-{depth}-{i}");
            row.Children.AddRange(MakeTree(depth - 1));
            return row;
        }).ToList();
    }

    private static MethodInfo Method(object instance, string name) =>
        instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static FieldInfo Field(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
}
