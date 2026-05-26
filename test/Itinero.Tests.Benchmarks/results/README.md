# Benchmark results log

This folder collects BenchmarkDotNet results from snapping / PBF-build runs.
Every run that's worth keeping (a baseline, a refactor's before/after, a
regression investigation) gets its own markdown file. Together they form a
rough timeline of how performance has moved.

This is a manual convention, not a tool. Nobody enforces it. The goal is just
that when someone six months from now asks "is snapping faster than it was?",
the answer is a `git log results/` away.

## When to log a result

Log when:

- You've established a new baseline on a benchmark you'll iterate against.
- You finished a perf-related change and want to record the before/after.
- You investigated a suspected regression and have numbers either way.

Don't log:

- Local "did I break it?" runs. Re-run yourself.
- Sweeps over knobs that aren't on the main branch.

## File naming

```
YYYY-MM-DD_<short-sha>_<short-label>.md
```

Examples:

- `2026-05-22_a1b2c3d_baseline-snap.md`
- `2026-05-30_e4f5g6h_after-bbox-prefilter.md`
- `2026-06-04_i7j8k9l_pbf-build-baseline.md`

The date is the run date. The SHA is `git rev-parse --short HEAD` at run
time. The label is a few words about what's being measured or what changed.

## File format

Each file starts with a YAML frontmatter block followed by the BDN summary
table and any notes. Copy [`template.md`](./template.md) to start. Keep it
short — the BDN output is the evidence.

The frontmatter fields are not validated; they exist to make `grep` useful.

## How to run

Snapping benchmarks (fast — minutes):

```sh
dotnet run -c Release --project test/Itinero.Tests.Benchmarks \
    -- --filter "*SnappingBenchmarks*"
```

PBF build benchmarks, Luxembourg only (slow — ~minutes per iteration):

```sh
dotnet run -c Release --project test/Itinero.Tests.Benchmarks \
    -- --filter "*PbfBuildBenchmarks*" --anyCategories=fast
```

PBF build benchmarks, Belgium variant (very slow — opt-in only):

```sh
dotnet run -c Release --project test/Itinero.Tests.Benchmarks \
    -- --filter "*PbfBuildBenchmarks*" --anyCategories=slow
```

BDN writes a fuller report to `BenchmarkDotNet.Artifacts/` next to wherever
you ran it. The markdown summary in the console output is what goes into
the results file below. Keep the artifacts dir out of git.
