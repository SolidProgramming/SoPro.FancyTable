using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SoPro.FancyTable.BrowserBenchmarks;

internal sealed class HostProcess : IDisposable
{
    private readonly Process process;
    private readonly ConcurrentQueue<string> logs = new();
    private readonly TaskCompletionSource<Uri> address = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly string logPath;
    public int Id => process.Id;

    public HostProcess(string root, string directory)
    {
        logPath = Path.Combine(directory, "server.log");
        string project = Path.Combine(root, "benchmarks", "SoPro.FancyTable.BrowserHost");
        string dll = Path.Combine(project, "bin", "Release", "net10.0", "SoPro.FancyTable.BrowserHost.dll");
        if (!File.Exists(dll)) throw new FileNotFoundException("Build the browser benchmark project in Release first.", dll);
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = project, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(dll);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        process = new Process { StartInfo = start, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) => Capture(args.Data);
        process.ErrorDataReceived += (_, args) => Capture(args.Data);
        process.Exited += (_, _) => address.TrySetException(new InvalidOperationException("Benchmark host exited during startup."));
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private void Capture(string? line)
    {
        if (line is null) return;
        logs.Enqueue(line);
        Match match = Regex.Match(line, @"Now listening on: (http://127\.0\.0\.1:\d+)");
        if (match.Success) address.TrySetResult(new Uri(match.Groups[1].Value));
    }

    public async Task<Uri> ReadyAsync(CancellationToken cancellation)
    {
        Uri uri = await address.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellation);
        await WaitHttpAsync(new Uri(uri, "/health"), TimeSpan.FromSeconds(30), cancellation);
        return uri;
    }

    internal static async Task WaitHttpAsync(Uri uri, TimeSpan timeout, CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(timeout);
        using var client = new HttpClient { Timeout = timeout };
        try
        {
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync(uri, deadline.Token);
                    if (response.IsSuccessStatusCode) return;
                }
                catch (HttpRequestException) { }
                await Task.Delay(50, deadline.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            throw new TimeoutException($"Host did not become ready: {uri}");
        }
    }

    public void SaveLogs() => File.WriteAllLines(logPath, logs);

    public void Dispose()
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        finally
        {
            SaveLogs();
            process.Dispose();
        }
    }
}
