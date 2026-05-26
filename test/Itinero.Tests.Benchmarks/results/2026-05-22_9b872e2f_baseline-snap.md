---
date: 2026-05-22
commit: 9b872e2f
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: Itinero defaults — OffsetInMeter=100, OffsetInMeterMax=500, MaxDistance=100
label: NOT a real-world baseline — uses Itinero defaults, not publish-api settings
superseded_by: 2026-05-22_<sha>_baseline-snap-publishapi.md
---

> **Caveat.** These numbers were taken with the Itinero default snap
> settings (100m / 500m / 100m), which search ~0.04 km². Real services
> like publish-api run with 5000m / 5000m / ∞, which sweeps ~100 km² —
> ~2500× more area, and dramatically more candidate edges per snap in
> dense networks. Do not use these numbers as a baseline for the snap
> refactor; see the publish-api-settings file referenced above instead.
> Kept here as a reference for what the default-settings cost looks like.

## Summary

First end-to-end baseline of the snapping pipeline against Belgium. Two
things are immediately visible and match the algorithmic analysis from
earlier in this thread:

- **Network density dominates.** Dense urban (Brussels) is ~200× slower
  than the rural rows: 2.02 ms vs ~10 µs per snap, with 1.2 MB allocated
  vs ~10 KB. Coast-brugge is in between at 537 µs / 296 KB. This is
  consistent with the "every edge in the box gets fully geometry-tested"
  pattern in `EdgeSearch.SnapInBoxAsync` — there's no MBR prefilter, so
  dense tiles pay full freight per snap.
- **Aggregate throughput is ~14k snaps/sec** on the spread of 50
  Halton-distributed points across Flanders: 3.6 ms for the full sweep,
  with 2.3 MB allocated total. The aggregate masks the per-location
  spread but it's the headline number we want refactors to move.

The offshore-miss case (LocationIndex=5) confirms the no-hit path exits
fast (~720 ns) and barely allocates — that path is already fine.

## Results

```
| Method                 | LocationIndex | Mean           | Allocated  |
|----------------------- |-------------- |---------------:|-----------:|
| ToAsync_Single         | 0 urban-brussels        |  2,019.5 us  | 1227.76 KB |
| ToAllAsync_Drain       | 0 urban-brussels        |  1,144.0 us  |  964.33 KB |
| ToVertexAsync_Single   | 0 urban-brussels        |  1,198.3 us  | 1229.56 KB |
| ToAsync_Single         | 1 suburb-zellik         |     31.1 us  |   18.25 KB |
| ToAllAsync_Drain       | 1 suburb-zellik         |    135.4 us  |   78.94 KB |
| ToVertexAsync_Single   | 1 suburb-zellik         |     58.8 us  |   49.37 KB |
| ToAsync_Single         | 2 village-wechelderzande|     47.2 us  |   25.26 KB |
| ToAllAsync_Drain       | 2 village-wechelderzande|     31.8 us  |   22.02 KB |
| ToVertexAsync_Single   | 2 village-wechelderzande|     18.9 us  |   15.87 KB |
| ToAsync_Single         | 3 rural-vorselaar       |     10.0 us  |    9.57 KB |
| ToAllAsync_Drain       | 3 rural-vorselaar       |      9.3 us  |   10.12 KB |
| ToVertexAsync_Single   | 3 rural-vorselaar       |     15.5 us  |   15.87 KB |
| ToAsync_Single         | 4 coast-brugge          |    537.4 us  |  296.49 KB |
| ToAllAsync_Drain       | 4 coast-brugge          |    342.0 us  |   232.1 KB |
| ToVertexAsync_Single   | 4 coast-brugge          |    197.3 us  |  170.05 KB |
| ToAsync_Single         | 5 offshore-miss         |      0.72 us |    2.56 KB |
| ToAllAsync_Drain       | 5 offshore-miss         |      0.43 us |    2.06 KB |
| ToVertexAsync_Single   | 5 offshore-miss         |      0.36 us |    1.43 KB |
| ToAsync_Single         | 6 motorway-north        |      9.1 us  |    7.13 KB |
| ToAllAsync_Drain       | 6 motorway-north        |      8.6 us  |    7.46 KB |
| ToVertexAsync_Single   | 6 motorway-north        |      7.3 us  |    5.87 KB |
| ToAsync_Single         | 7 motorway-south        |     11.1 us  |   12.95 KB |
| ToAllAsync_Drain       | 7 motorway-south        |      6.3 us  |    4.20 KB |
| ToVertexAsync_Single   | 7 motorway-south        |      7.6 us  |   10.05 KB |
| ToAsync_Spread50       | (50 Halton points)      |  3,605–3,810 us | 2335.83 KB |
| ToVertexAsync_Spread50 | (50 Halton points)      |  2,244–2,290 us | 2283.42 KB |
```

Spread50 results vary only ±5% across LocationIndex values, as expected:
the param is unused by those methods, so this is pure noise floor /
measurement repeatability. Reading those as a single number per method
is fine.

## Notes

- Full BDN summary saved at `BenchmarkDotNet.Artifacts/` (gitignored).
- Total run time 29:43 for 40 benchmark cases.
- Belgium routerdb was loaded from disk (~10s); no PBF rebuild this run.
- Pre-warming in `[GlobalSetup]` worked — variance per-location is low
  (StdDev < 1% of Mean on most rows), so this isn't measuring tile-load
  cost.
- Apple M1 is significantly slower at .NET workloads than recent x86
  hardware; numbers from a different machine will differ. Always compare
  before/after on the same host.
