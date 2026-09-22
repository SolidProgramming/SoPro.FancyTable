using SoPro.FancyTable.BrowserBenchmarks;
using Xunit;

namespace SoPro.FancyTable.Tests;

public sealed class BrowserReportTests
{
    [Fact]
    public void Statistics_use_median_and_nearest_rank_p95()
    {
        Assert.Null(Statistics.Calculate([]));
        Assert.Equal(new Statistics(1, 7, 7, 7, 7), Statistics.Calculate([7]));
        Assert.Equal(new Statistics(4, 2.5, 4, 1, 4), Statistics.Calculate([4, 1, 3, 2]));
        Assert.Equal(new Statistics(20, 10.5, 19, 1, 20), Statistics.Calculate(Enumerable.Range(1, 20).Select(i => (double)i)));
        Assert.Throws<ArgumentException>(() => Statistics.Calculate([double.NaN]));
    }

    [Fact]
    public void Matrix_contains_all_48_actions_and_exact_ten_percent_search_matches()
    {
        var fixtures = Fixture.All();
        Assert.Equal(12, fixtures.Count);
        Assert.Equal(48, fixtures.Sum(f => f.Actions.Length));
        foreach (var fixture in fixtures.Where(f => f.Kind == "flat" && !f.Paged))
            Assert.Equal(fixture.Size / 10, fixture.Expected("search").Length);
        foreach (var fixture in fixtures.Where(f => f.Kind == "tree"))
        {
            Assert.Equal(10, fixture.Expected("mount").Length);
            Assert.Equal(20, fixture.Expected("expand").Length);
            Assert.Equal(fixture.Size == 3 ? 1110 : 11110, fixture.Expected("expand-all").Length);
        }
    }
}
