# Playwright browser performance benchmarks

This harness measures the real `FancyTable` and `FancyTreeTable` components in a
local Blazor Interactive Server app without prerendering. It includes Blazor
rendering, DOM updates, browser layout, and local SignalR transport. It does not
isolate browser CPU time and does not measure scrolling or WebAssembly.

## Setup and execution

Install the Chromium version matching Microsoft.Playwright 1.62.0 once:

```shell
dotnet run --project benchmarks/SoPro.FancyTable.BrowserBenchmarks/SoPro.FancyTable.BrowserBenchmarks.csproj -c Release -- --install
```

Linux CI or a new Linux workstation can include native dependencies with
`--install --with-deps`. Then run one of the profiles:

```shell
# Functional matrix check: 0 warmups, 1 measured repetition.
dotnet run --project benchmarks/SoPro.FancyTable.BrowserBenchmarks/SoPro.FancyTable.BrowserBenchmarks.csproj -c Release -- --profile Dry

# Useful local report: 2 warmups, 5 measured repetitions.
dotnet run --project benchmarks/SoPro.FancyTable.BrowserBenchmarks/SoPro.FancyTable.BrowserBenchmarks.csproj -c Release -- --profile Short

# Longer local report: 5 warmups, 30 measured repetitions.
dotnet run --project benchmarks/SoPro.FancyTable.BrowserBenchmarks/SoPro.FancyTable.BrowserBenchmarks.csproj -c Release -- --profile Full
```

Add `--headed` to watch Chromium. In VS Code the same commands are available
through **Terminal > Run Task** as `Browser Benchmarks: Install Chromium`, `Dry`,
`Short`, and `Full`.

## Measurement model

The runner builds and starts its own Release test host on a random loopback port,
waits for HTTP and an interactive Blazor event, launches Chromium at 1440 × 900,
and executes all scenarios sequentially. Test data, reset, connection setup, and
preparation needed for collapse actions are outside the timed section.

An event-capture listener calls `performance.now()` immediately when the click or
input reaches the page. A `MutationObserver` checks row count, first/last row ID,
and expansion state. Two durations are recorded:

- `DomMs`: event until the expected DOM state.
- `FrameMs`: event until two subsequent `requestAnimationFrame` callbacks. This is
  an opportunity to paint, not proof that pixels reached the display.

After timing, Playwright verifies every row ID and order and all relevant tree
expansion states. Each operation has a 30-second timeout. Browser errors, failed
requests, connection loss, timeouts, and incorrect results abort the run without
retrying the sample. The runner kills the host process in `finally`.

Search uses a single Playwright `FillAsync("needle")` call, so the component's
existing 250 ms debounce is included. The deterministic category data makes
exactly ten percent of flat rows match.

## Scenarios and reports

Flat tables cover 1,000 and 10,000 items, text and templated cells, and pagination
with 50 rows or all rows. Actions are mount, sort, search, and page 2 where
applicable. Trees cover 1,110 and 11,110 nodes, both cell variants, and mount,
expand/collapse first root, and expand/collapse all.

Every run creates a unique directory under `BrowserBenchmark.Artifacts/` with:

- `results.json`, including every warmup and measurement plus environment data;
- `index.html`, with success/failure counts and median, nearest-rank p95, minimum,
  and maximum for both durations; warmups are excluded from statistics;
- browser and server logs, plus screenshots when a failure occurs.

Reports are ignored by Git. There are deliberately no time-based pass/fail limits.
CI installs Chromium, runs the full scenario matrix with `Dry`, and uploads the
report even on failure. For stable comparisons use `Short` or `Full` on the same
machine, runtime, power configuration, and browser mode without competing work.

Local fixture assets are Bootstrap 5.3.7, Bootstrap Icons 1.11.3, and Inter 5.2.5.
Their sources, SHA-256 hashes, and licenses are stored under the host's
`wwwroot/vendor` directory; the measured page makes no CDN requests.
