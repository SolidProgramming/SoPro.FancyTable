# Data preparation benchmarks

Run from the repository root with .NET 10, in Release mode, without a debugger.
In VS Code, open this repository folder and choose **Terminal > Run Task**:
`Benchmarks: Dry (all)`, `Benchmarks: Short (all)`,
`Benchmarks: Full (flat table)`, or `Benchmarks: Full (tree table)`.
These tasks run the corresponding commands below
from the repository root.

BenchmarkDotNet launches isolated processes, warms up the code, and reports time
and allocations via `MemoryDiagnoser`. NuGet access may be needed for its generated
benchmark projects even after the main project has been built.

```shell
# Quick functional check of every benchmark; not a meaningful timing comparison.
dotnet run --project benchmarks/SoPro.FancyTable.Benchmarks/SoPro.FancyTable.Benchmarks.csproj -c Release -- --filter "*" --job Dry

# Short measurement run.
dotnet run --project benchmarks/SoPro.FancyTable.Benchmarks/SoPro.FancyTable.Benchmarks.csproj -c Release -- --filter "*" --job Short

# Full default measurement run, flat table only.
dotnet run --project benchmarks/SoPro.FancyTable.Benchmarks/SoPro.FancyTable.Benchmarks.csproj -c Release -- --filter "*FlatPreparationBenchmarks*"

# Full default measurement run, tree table only.
dotnet run --project benchmarks/SoPro.FancyTable.Benchmarks/SoPro.FancyTable.Benchmarks.csproj -c Release -- --filter "*TreePreparationBenchmarks*"
```

Markdown, CSV, and HTML reports are generated under
`BenchmarkDotNet.Artifacts/results/` in the working directory and ignored by Git.
Compare `Mean`, `Error`, `Ratio`, `Allocated`, and the GC columns. For conclusions
about small differences, use the default job rather than `Short`, repeat on the
same machine/runtime/power settings, and avoid competing workloads. The harness
preserves the machine's existing power plan.

## Cases

| Benchmark | Inputs | Measured operation |
|---|---|---|
| Flat `UncachedQuery` (baseline) | 1,000 / 10,000 / 100,000 rows | Clear query cache, search all rows, sort reversed names ascending, materialize results, prepare first page of 25 rows |
| Flat `CachedQuery` | Same inputs, cache populated in setup | Reuse filtered/sorted rows and prepare the first page |
| Tree `OriginalSearch` (baseline) | 1,110 / 11,110 nodes | Original traversal with a temporary child-results list per node |
| Tree `UncachedSearch` | Same tree | Clear visible-node cache and execute the current traversal |
| Tree `CachedSearch` | Same tree, cache populated in setup | Retrieve the visible-node list |

Trees have ten roots, ten children per non-leaf, and three or four levels.
The `row` filter matches every node. The `match` filter matches one leaf in each
group of ten siblings; retained ancestors must remain in depth-first order.
Setup verifies that original and current searches return the same node sequence.

## Measurement boundaries

- Setup creates real FancyTable components and invokes their parameter lifecycle
  once. Data creation, initial tree construction, and cache warming are excluded.
- Private preparation methods are bound to delegates in setup. Cache-clearing
  delegates are compiled once. Measured operations use no `MethodInfo.Invoke` or
  `FieldInfo.SetValue`. Changes to private members must update this harness.
- Uncached cases clear the relevant cache **on every invocation**, including
  warmup. Clearing is included in their reported cost. The flat baseline uses
  current code with caching defeated, not a historical binary.
- The tree baseline preserves the original search algorithm and uses the same
  predicate as the component. It excludes the initial tree build, sibling sorting,
  expansion statistics, and pagination, as do both current tree-search cases.
- These are synchronous .NET preparation benchmarks. They exclude Blazor
  rendering, DOM/layout, JS interop, networking, and user-perceived scroll latency.
  Cached tree retrieval is extremely small; its timing can approach measurement
  overhead. Its main evidence is the absence of traversal and allocations.
- BenchmarkDotNet is a dependency of this executable only. No benchmark code or
  dependencies are shipped in the component package. Timing thresholds are not
  part of CI; bUnit/xUnit continue checking behavior and selector counts.

## Example ShortRun

Measured on 2026-09-21 with BenchmarkDotNet 0.15.8, .NET 10.0.12, SDK 10.0.401,
Windows x64, and an AMD Ryzen 7 7800X3D. Three warmup and three measurement
iterations, one launch; values below are rounded. These are observations from
one short run, not performance guarantees or CI thresholds.

| Case | Mean | Allocated per operation |
|---|---:|---:|
| Flat, 10,000 rows, uncached | 5.39 ms | 601,328 B |
| Flat, 10,000 rows, cached | 61 ns | 720 B |
| Tree, 11,110 nodes, all match, original | 298 µs | 1,494,725 B |
| Tree, 11,110 nodes, all match, new uncached | 288 µs | 618,004 B |
| Tree, 11,110 nodes, sparse matches, original | 136 µs | 575,496 B |
| Tree, 11,110 nodes, sparse matches, new uncached | 197 µs | 421,320 B |

Cached tree retrieval reported no allocations. Its approximately 1–2 ns timing
is too small to use as a practical UI speedup claim. Dense uncached tree timing
had wide error intervals, so the small mean difference is inconclusive. Sparse
uncached search was slower in this comparison despite allocating less memory;
fewer allocations alone do not establish a CPU improvement. Use a full default
run and profiling before optimizing that case further.

See the official [command-line guide](https://benchmarkdotnet.org/articles/guides/console-args.html)
and [diagnoser documentation](https://benchmarkdotnet.org/articles/configs/diagnosers.html).
