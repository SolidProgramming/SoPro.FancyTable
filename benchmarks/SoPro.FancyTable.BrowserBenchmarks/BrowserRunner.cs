using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;

namespace SoPro.FancyTable.BrowserBenchmarks;

internal sealed class BrowserRunner(IBrowser browser, Uri address, RunReport report, string directory)
{
    private readonly ConcurrentQueue<string> logs = new();
    private readonly ConcurrentQueue<string> browserErrors = new();
    private bool connectionLost;

    private static string Selector(string action) => action switch
    {
        "mount" => "#mount",
        "sort" => "#table-region th[data-column-key='name'] .btn-link",
        "search" => "#table-region input",
        "page" => "#table-region .fancy-pagination button:last-child",
        "expand" or "collapse" => "#table-region tbody tr:first-child .tree-toggle-btn",
        "expand-all" => "#expand-all",
        "collapse-all" => "#collapse-all",
        _ => throw new ArgumentException($"Unknown action: {action}")
    };

    public async Task RunAsync(Profile profile, CancellationToken cancellation)
    {
        browser.Disconnected += (_, _) => connectionLost = true;
        foreach (Fixture fixture in Fixture.All())
        {
            cancellation.ThrowIfCancellationRequested();
            await using var context = await NewContextAsync();
            IPage page = await context.NewPageAsync();
            Observe(page);
            try
            {
                await OpenAsync(page, fixture);
                foreach (string action in fixture.Actions)
                {
                    Console.WriteLine($"{fixture.Id}/{action}: {profile.Warmups} warmup + {profile.Measurements} measured");
                    for (int i = 0; i < profile.Warmups + profile.Measurements; i++)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        bool warmup = i < profile.Warmups;
                        int iteration = warmup ? i + 1 : i - profile.Warmups + 1;
                        Timing? timing = null;
                        try
                        {
                            ThrowBrowserErrors();
                            await ResetAsync(page, fixture, action);
                            timing = await MeasureAsync(page, fixture, action);
                            await VerifyAsync(page, fixture, action);
                            ThrowBrowserErrors();
                            report.Samples.Add(new(fixture.Id, action, iteration, warmup, timing.DomMs, timing.FrameMs, null));
                        }
                        catch (Exception exception)
                        {
                            report.Samples.Add(new(fixture.Id, action, iteration, warmup, timing?.DomMs, timing?.FrameMs, exception.ToString()));
                            await CaptureFailureAsync(page, $"{fixture.Id}-{action}-{iteration}");
                            report.Save(directory);
                            // Abort instead of retrying or mixing a broken circuit into statistics.
                            throw;
                        }
                    }
                    report.Save(directory);
                }
            }
            catch
            {
                await CaptureFailureAsync(page, fixture.Id + "-failure");
                throw;
            }
            finally { SaveLogs(); }
        }
        ThrowBrowserErrors();
    }

    private async Task<IBrowserContext> NewContextAsync() => await browser.NewContextAsync(new()
    {
        ViewportSize = new() { Width = 1440, Height = 900 }, DeviceScaleFactor = 1,
        Locale = "en-US", TimezoneId = "UTC", ReducedMotion = ReducedMotion.Reduce,
        ServiceWorkers = ServiceWorkerPolicy.Block
    });

    private void Observe(IPage page)
    {
        page.PageError += (_, error) => browserErrors.Enqueue(error);
        page.Crash += (_, _) => browserErrors.Enqueue("Browser page crashed.");
        page.Console += (_, message) =>
        {
            logs.Enqueue($"{message.Type}: {message.Text}");
            if (message.Type == "error") browserErrors.Enqueue(message.Text);
        };
        page.RequestFailed += (_, request) => browserErrors.Enqueue($"Request failed: {request.Url}: {request.Failure}");
        page.Response += (_, response) =>
        {
            if (response.Status >= 400) browserErrors.Enqueue($"HTTP {response.Status}: {response.Url}");
        };
        page.Request += (_, request) =>
        {
            if (Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https" && uri.Authority != address.Authority)
                browserErrors.Enqueue($"External request is not allowed in benchmark fixture: {request.Url}");
        };
    }

    private async Task OpenAsync(IPage page, Fixture fixture)
    {
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(new Uri(address, fixture.Query).ToString());
        await page.Locator("#ping").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#fixture')?.dataset.ping === '1'");
        await page.EvaluateAsync("async () => { await Promise.all([document.fonts.load('16px BenchmarkSans'), document.fonts.load('16px bootstrap-icons')]); await document.fonts.ready; }");
        ThrowBrowserErrors();
    }

    private static async Task WaitDomAsync(IPage page, Fixture fixture, string action)
    {
        await page.WaitForFunctionAsync("expected => window.benchmark.matches(expected)", fixture.ExpectedDom(action));
        await page.EvaluateAsync("() => window.benchmark.settle()");
    }

    private static async Task ResetAsync(IPage page, Fixture fixture, string action)
    {
        await page.Locator("#reset").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#fixture').dataset.mounted === 'false' && !document.querySelector('#table-region table')");
        if (action != "mount")
        {
            await page.Locator("#mount").ClickAsync();
            await WaitDomAsync(page, fixture, "mount");
        }
        if (action is "collapse" or "collapse-all")
        {
            string prepare = action == "collapse" ? "expand" : "expand-all";
            await page.Locator(Selector(prepare)).ClickAsync();
            await WaitDomAsync(page, fixture, prepare);
        }
        await page.EvaluateAsync("() => window.benchmark.settle()");
    }

    private static async Task<Timing> MeasureAsync(IPage page, Fixture fixture, string action, bool impossible = false)
    {
        object expected = impossible ? new { count = -1, first = "absent", last = "absent" } : fixture.ExpectedDom(action);
        await page.EvaluateAsync("options => window.benchmark.arm(options)", new
        {
            selector = Selector(action), @event = action == "search" ? "input" : "click",
            expected, timeout = impossible ? 250 : 30_000
        });
        if (action == "search") await page.Locator(Selector(action)).FillAsync("needle");
        else await page.Locator(Selector(action)).ClickAsync();
        JsonElement result = await page.EvaluateAsync<JsonElement>("() => window.benchmark.result()");
        if (result.TryGetProperty("error", out var error)) throw new TimeoutException(error.GetString());
        double dom = result.GetProperty("domMs").GetDouble(), frame = result.GetProperty("frameMs").GetDouble();
        if (!double.IsFinite(dom) || !double.IsFinite(frame) || dom < 0 || frame < dom)
            throw new InvalidOperationException("Invalid browser timing result.");
        return new(dom, frame, null);
    }

    private static async Task VerifyAsync(IPage page, Fixture fixture, string action)
    {
        string[] actual = await page.Locator("#table-region tbody tr")
            .EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.rowId)");
        if (!actual.SequenceEqual(fixture.Expected(action)))
            throw new InvalidOperationException($"Unexpected row IDs/order: {fixture.Id}/{action}.");
        if (fixture.Kind == "tree")
        {
            bool[] expanded = await page.Locator("#table-region .tree-toggle-btn")
                .EvaluateAllAsync<bool[]>("buttons => buttons.map(button => button.querySelector('.bi-caret-down-fill') !== null)");
            int expectedCount = action == "expand-all" ? (fixture.Size == 3 ? 110 : 1110) : action == "expand" ? 1 : 0;
            if (expanded.Count(value => value) != expectedCount)
                throw new InvalidOperationException("Unexpected expansion state.");
        }
    }

    private void ThrowBrowserErrors()
    {
        if (connectionLost) throw new InvalidOperationException("Browser disconnected.");
        if (!browserErrors.IsEmpty) throw new InvalidOperationException(string.Join("\n", browserErrors));
    }

    private async Task CaptureFailureAsync(IPage page, string name)
    {
        try { await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), Timeout = 5000 }); }
        catch (Exception exception) { logs.Enqueue("Screenshot failed: " + exception.Message); }
        SaveLogs();
    }

    public void SaveLogs() => File.WriteAllLines(Path.Combine(directory, "browser.log"), logs.Concat(browserErrors));

    public async Task VerifyTimeoutAsync()
    {
        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();
        Observe(page);
        var fixture = Fixture.All()[0];
        await OpenAsync(page, fixture);
        try
        {
            await MeasureAsync(page, fixture, "mount", impossible: true);
            throw new InvalidOperationException("Expected DOM timeout was not raised.");
        }
        catch (TimeoutException exception) when (exception.Message.Contains("expected table DOM"))
        {
            logs.Enqueue("Expected failure check passed: " + exception.Message);
            await CaptureFailureAsync(page, "expected-dom-timeout");
        }
        ThrowBrowserErrors();
    }
}
