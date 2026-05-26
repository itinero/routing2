---
date: 2026-05-22
commit: 9b872e2f
branch: develop
benchmarks: PbfBuildBenchmarks.BuildLuxembourg
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/luxembourg-latest.osm.pbf (51 MB, 2026-03-18)
label: baseline PBF→RouterDb build (Luxembourg only)
---

## Summary

First baseline for the PBF→RouterDb import pipeline. Luxembourg is the
"fast" target — small enough to iterate against, large enough to be
representative of the per-element parse + tile + edge construction work.

Headline: **6.2 s** to build the full RouterDb from a 51 MB PBF, with
**7.68 GB allocated** along the way (~150× the input PBF size). Almost
all of that is OSM stream parsing churn (raw node/way/relation objects,
tag collections, intermediate dictionaries) — most of it is short-lived
Gen0 garbage but the Gen2 numbers (49k collects per build) tell you
some of it survives long enough to matter for sustained perf.

This isn't the primary target of the snap refactor, but it sets the
ceiling on cold-start latency in real services, so it's worth tracking.

## Results

```
| Method          | Mean    | Error   | StdDev   | Gen0         | Gen1        | Gen2       | Allocated |
|---------------- |--------:|--------:|---------:|-------------:|------------:|-----------:|----------:|
| BuildLuxembourg | 6.207 s | 1.726 s | 0.0946 s | 1270000.0000 | 492000.0000 | 49000.0000 |   7.68 GB |
```

Confidence-interval margin is 27.8% of mean — high, because BDN only ran
3 iterations + 1 warmup (intentional; PBF builds are slow). The StdDev is
small (~1.5% of mean) so the run is stable, the CI is just wide because
N=3. Treat the 6.2 s as the headline and the 1.7 s margin as "don't
sweat sub-second differences in single-run comparisons."

## Notes

- Belgium variant (`PbfBuildBenchmarks.BuildBelgium`, in `--anyCategories=slow`)
  was not run — Belgium PBF is 14× the size of Luxembourg, build time would
  be many minutes per iteration. Run separately when needed.
- `RunStrategy.Monitoring` + `iterationCount=3` chosen so the benchmark
  finishes in well under a minute total. Default BDN config would have
  run far more iterations and made this impractical.
- A previous attempt failed with `FileNotFoundException` because
  `ResolvePath` only checked `cwd` and `cwd/test/Itinero.Tests.Functional/`,
  but BDN spawns workers in `bin/Release/net10.0/<guid>/bin/Release/net10.0/`.
  Fixed by walking up from `AppContext.BaseDirectory` to find the test
  data directory.
- 32 s total wall-clock for the BDN run (1 warmup + 3 iterations + setup).
