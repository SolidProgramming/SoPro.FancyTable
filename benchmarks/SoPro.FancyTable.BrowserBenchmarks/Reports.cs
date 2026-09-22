using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SoPro.FancyTable.BrowserBenchmarks;

public sealed record Statistics(int Count, double Median, double P95, double Minimum, double Maximum)
{
    public static Statistics? Calculate(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        if (sorted.Length == 0) return null;
        if (sorted.Any(value => !double.IsFinite(value) || value < 0))
            throw new ArgumentException("Durations must be finite and nonnegative.");
        int middle = sorted.Length / 2;
        double median = sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) / 2 : sorted[middle];
        return new(sorted.Length, median, sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], sorted[0], sorted[^1]);
    }
}

public sealed record Timing(double DomMs, double FrameMs, string? Error);
public sealed record Sample(string Scenario, string Action, int Iteration, bool Warmup,
    double? DomMs, double? FrameMs, string? Error);
public sealed class RunReport
{
    public Dictionary<string, object?> Metadata { get; } = [];
    public List<Sample> Samples { get; } = [];
    public List<string> Errors { get; } = [];
    public bool Complete { get; set; }

    public void Save(string directory)
    {
        File.WriteAllText(Path.Combine(directory, "results.json"), JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
        static string Encode(string text) => WebUtility.HtmlEncode(text);
        static string Cells(Statistics? stats) => stats is null ? "<td colspan='4'>—</td>" :
            string.Concat(new[] { stats.Median, stats.P95, stats.Minimum, stats.Maximum }
                .Select(value => $"<td>{value.ToString("F3", CultureInfo.InvariantCulture)}</td>"));
        var html = new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><title>FancyTable browser benchmarks</title><style>body{font:14px system-ui;margin:2rem}table{border-collapse:collapse}td,th{padding:.5rem;border:1px solid #aaa;text-align:right}td:first-child{text-align:left}pre{white-space:pre-wrap}.error{color:#a00}</style><h1>FancyTable browser benchmarks</h1>");
        html.Append($"<p>Complete: {Complete}. Samples: {Samples.Count}. Warmups are retained in JSON but excluded from statistics.</p>");
        html.Append("<p>Milliseconds from the browser event to expected DOM and to two subsequent animation-frame callbacks (a frame opportunity, not guaranteed pixel presentation). Includes local Interactive Server/SignalR work. No timing budgets. p95 uses nearest rank; Dry is a functional check only.</p>");
        html.Append("<table><tr><th>Scenario / action</th><th>Success</th><th>Failed</th><th>DOM median</th><th>DOM p95</th><th>DOM min</th><th>DOM max</th><th>Frame median</th><th>Frame p95</th><th>Frame min</th><th>Frame max</th></tr>");
        foreach (var group in Samples.Where(s => !s.Warmup).GroupBy(s => (s.Scenario, s.Action)))
        {
            var good = group.Where(s => s.Error is null).ToArray();
            html.Append($"<tr><td>{Encode(group.Key.Scenario + " / " + group.Key.Action)}</td><td>{good.Length}</td><td>{group.Count() - good.Length}</td>");
            html.Append(Cells(Statistics.Calculate(good.Select(s => s.DomMs!.Value))));
            html.Append(Cells(Statistics.Calculate(good.Select(s => s.FrameMs!.Value))));
            html.Append("</tr>");
        }
        html.Append("</table><h2>Errors (including warmups and setup)</h2><pre class='error'>");
        html.Append(Encode(string.Join("\n", Errors.Concat(Samples.Where(s => s.Error is not null)
            .Select(s => $"{s.Scenario}/{s.Action}/{s.Iteration} (warmup={s.Warmup}): {s.Error}")))));
        html.Append("</pre><h2>Environment</h2><pre>");
        html.Append(Encode(JsonSerializer.Serialize(Metadata, new JsonSerializerOptions { WriteIndented = true })));
        html.Append("</pre></html>");
        File.WriteAllText(Path.Combine(directory, "index.html"), html.ToString());
    }
}
