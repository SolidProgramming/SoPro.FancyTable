using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Playwright;
using SoPro.FancyTable.BrowserBenchmarks;

if (args.Contains("--install"))
{
    return Microsoft.Playwright.Program.Main(args.Contains("--with-deps")
        ? ["install", "--with-deps", "chromium"]
        : ["install", "chromium"]);
}

string root = FindRoot();
string directory = Path.Combine(root, "BrowserBenchmark.Artifacts", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(directory);
var report = new RunReport();
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
HostProcess? host = null;
BrowserRunner? runner = null;
try
{
    bool headed = false, verifyFailures = false;
    string profileName = "Short";
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--profile" when i + 1 < args.Length: profileName = args[++i]; break;
            case "--headed": headed = true; break;
            case "--verify-failures": verifyFailures = true; break;
            default: throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
        }
    }
    var profile = Profile.Parse(profileName);
    report.Metadata["startedUtc"] = DateTimeOffset.UtcNow;
    report.Metadata["commit"] = Git(root, "rev-parse", "HEAD");
    report.Metadata["dirtyWorkingTree"] = !string.IsNullOrWhiteSpace(Git(root, "status", "--porcelain"));
    report.Metadata["os"] = RuntimeInformation.OSDescription;
    report.Metadata["architecture"] = RuntimeInformation.ProcessArchitecture.ToString();
    report.Metadata["dotnet"] = RuntimeInformation.FrameworkDescription;
    report.Metadata["playwright"] = typeof(IPlaywright).Assembly.GetName().Version?.ToString(3);
    report.Metadata["browserMode"] = headed ? "headed" : "headless";
    report.Metadata["viewport"] = new { width = 1440, height = 900, deviceScaleFactor = 1 };
    report.Metadata["locale"] = "en-US";
    report.Metadata["timezone"] = "UTC";
    report.Metadata["hosting"] = "Interactive Server, loopback, no prerendering";
    report.Metadata["profile"] = profile;
    report.Metadata["fixtures"] = Fixture.All();
    report.Metadata["failureVerification"] = verifyFailures;
    report.Metadata["plannedMeasurementCount"] = verifyFailures ? 0 : Fixture.All().Sum(f => f.Actions.Length) * profile.Measurements;
    Console.WriteLine($"Reports: {directory}");

    if (verifyFailures)
    {
        try
        {
            await HostProcess.WaitHttpAsync(new Uri("http://127.0.0.1:1/health"), TimeSpan.FromMilliseconds(300), cancellation.Token);
            throw new InvalidOperationException("Unreachable-host test unexpectedly succeeded.");
        }
        catch (TimeoutException) { Console.WriteLine("Unreachable-host failure check passed."); }
    }

    host = new HostProcess(root, directory);
    Uri address = await host.ReadyAsync(cancellation.Token);
    report.Metadata["hostPid"] = host.Id;
    report.Metadata["hostAddress"] = address;
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = !headed });
    report.Metadata["chromium"] = browser.Version;
    runner = new BrowserRunner(browser, address, report, directory);
    if (verifyFailures) await runner.VerifyTimeoutAsync();
    else await runner.RunAsync(profile, cancellation.Token);
    report.Complete = true;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    report.Errors.Add(exception.ToString());
}
finally
{
    runner?.SaveLogs();
    if (host is not null)
    {
        int pid = host.Id;
        try
        {
            host.Dispose();
            bool alive;
            try { using var process = Process.GetProcessById(pid); alive = !process.HasExited; }
            catch (ArgumentException) { alive = false; }
            report.Metadata["hostStopped"] = !alive;
            if (alive) throw new InvalidOperationException("Owned benchmark host is still running.");
        }
        catch (Exception exception) { report.Errors.Add(exception.ToString()); report.Complete = false; }
    }
    report.Metadata["finishedUtc"] = DateTimeOffset.UtcNow;
    report.Save(directory);
}
Console.WriteLine($"Complete: {report.Complete}; successful measurements: {report.Samples.Count(s => !s.Warmup && s.Error is null)}; report: {Path.Combine(directory, "index.html")}");
return report.Complete && report.Errors.Count == 0 && report.Samples.All(s => s.Error is null) ? 0 : 1;

static string FindRoot()
{
    for (var current = new DirectoryInfo(Directory.GetCurrentDirectory()); current is not null; current = current.Parent)
        if (File.Exists(Path.Combine(current.FullName, "SoPro.FancyTable.csproj"))) return current.FullName;
    throw new DirectoryNotFoundException("Run from the FancyTable repository or a child directory.");
}

static string Git(string root, params string[] args)
{
    try
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)!;
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? result.Trim() : "unavailable";
    }
    catch { return "unavailable"; }
}
