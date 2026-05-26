---
date: 2026-05-23
commit: 9b872e2f + uncommitted EdgeSearch.cs changes
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: publish-api — OffsetInMeter=5000, OffsetInMeterMax=5000, MaxDistance=double.MaxValue
job: out-of-process, warmupCount=2, iterationCount=4
label: PR 1 — per-edge MBR prefilter in EdgeSearch.SnapInBoxAsync / SnapAllInBoxAsync
baseline: 2026-05-23_9b872e2f_baseline-snap-publishapi.md
---

## Summary

Per-edge MBR prefilter: before running the full per-shape-point projection
loop, walk each candidate edge's geometry once for min/max, then check if
the bbox's nearest point to the query could possibly beat the current
best snap. If not, skip the edge entirely.

No caching — the bbox is recomputed per snap. Inline, allocation-conscious
(reuses the Shape enumerator, no extra IEnumerable). Per the design
constraint: memory pressure on huge graphs trumps per-call CPU savings,
so no per-tile dictionary.

Key implementation detail: early-exit when `bestDistance >= 1e10` (i.e.
"effectively unbounded"). The original check used `double.IsInfinity`,
which misses `double.MaxValue` — the value publish-api actually passes
for MaxDistance. Without this fix, `SnapAllInBoxAsync` regressed ~20%
because the prefilter ran on every edge with no chance of pruning.

## Headline

**ToAsync_Single: 2.0–2.6× faster across all dense locations.**
urban-brussels drops from 182 ms → 86 ms. Allocations down ~28% for the
worst case (73 MB → 52 MB per snap).

**ToAllAsync_Drain: flat (within noise).** The prefilter has no shrinking
"best distance" for ToAllAsync — each call cares about all edges within
MaxDistance — so the early-exit kicks in immediately and we incur no
extra work.

## Results — ToAsync_Single

| Location | Baseline | After | Speedup | Alloc baseline | Alloc after |
|---|---:|---:|---:|---:|---:|
| 0 urban-brussels        | 181.91 ms | **85.58 ms** | **2.13×** | 73.0 MB | 52.4 MB |
| 1 suburb-zellik         |  11.30 ms |   4.91 ms | 2.30× | 3.51 MB | 2.60 MB |
| 2 village-wechelderzande|  14.23 ms |   6.41 ms | 2.22× | 4.39 MB | 3.25 MB |
| 3 rural-vorselaar       |  14.12 ms |   5.86 ms | 2.41× | 4.27 MB | 3.20 MB |
| 4 coast-brugge          |  44.13 ms |  19.82 ms | 2.23× | 15.5 MB | 11.1 MB |
| 5 offshore-miss         |   0.77 µs |   0.75 µs | flat  | 1.72 KB | 1.72 KB |
| 6 motorway-north        |  10.60 ms |   4.74 ms | 2.24× | 3.61 MB | 2.63 MB |
| 7 motorway-south        |   2.18 ms |   0.88 ms | 2.48× | 0.51 MB | 0.41 MB |
| 8 halton-00             |  65.10 ms |  31.69 ms | 2.05× | 23.5 MB | 17.3 MB |
| 9 halton-01             |  41.34 ms |  19.48 ms | 2.12× | 15.3 MB | 11.1 MB |
| 10 halton-02            |  10.24 ms |   4.34 ms | 2.36× | 2.97 MB | 2.26 MB |
| 11 halton-03            |  14.63 ms |   5.81 ms | 2.52× | 3.97 MB | 2.99 MB |
| 12 halton-04            |  18.43 ms |   7.87 ms | 2.34× | 5.70 MB | 4.25 MB |
| 13 halton-05            |  21.09 ms |   8.92 ms | 2.36× | 6.51 MB | 4.83 MB |
| 14 halton-06            |  20.89 ms |   8.75 ms | 2.39× | 6.17 MB | 4.59 MB |
| 15 halton-07            |  12.67 ms |   5.15 ms | 2.46× | 3.60 MB | 2.71 MB |
| 16 halton-08            |  59.63 ms |  26.76 ms | 2.23× | 21.3 MB | 15.5 MB |
| 17 halton-09            |  17.46 ms |   6.63 ms | 2.63× | 4.65 MB | 3.51 MB |
| 18 halton-10            |  17.68 ms |   7.46 ms | 2.37× | 5.34 MB | 3.99 MB |
| 19 halton-11            |  19.66 ms |   7.69 ms | 2.56× | 5.05 MB | 3.85 MB |
| 20 halton-12            |  26.80 ms |  11.18 ms | 2.40× | 8.42 MB | 6.23 MB |
| 21 halton-13            |  24.55 ms |  10.07 ms | 2.44× | 7.55 MB | 5.55 MB |
| 22 halton-14            |  13.16 ms |   5.41 ms | 2.43× | 3.88 MB | 2.94 MB |
| 23 halton-15            |  12.84 ms |   5.36 ms | 2.39× | 3.65 MB | 2.74 MB |
| 24 halton-16            |  21.38 ms |   9.44 ms | 2.27× | 7.26 MB | 5.32 MB |
| 25 halton-17            |  17.91 ms |   7.21 ms | 2.48× | 4.94 MB | 3.74 MB |
| 26 halton-18            |  18.51 ms |   7.47 ms | 2.48× | 5.28 MB | 3.93 MB |
| 27 halton-19            |  11.31 ms |   4.55 ms | 2.48× | 3.08 MB | 2.38 MB |

## Results — ToAllAsync_Drain (no regression)

Same range as baseline; bbox check early-exits because MaxDistance=∞
sentinel is now caught. Spot-checking:

| Location | Baseline | After | Ratio |
|---|---:|---:|---:|
| 0 urban-brussels    | 84.67 ms | 86.32 ms | 1.02× |
| 1 suburb-zellik     | 32.38 ms | 33.05 ms | 1.02× |
| 3 rural-vorselaar   |  5.75 ms |  6.00 ms | 1.04× |
| 4 coast-brugge      | 22.44 ms | 22.71 ms | 1.01× |

All within ~5% — measurement noise.

## Notes

- 16:41 wall-clock. Slightly over 10-min target; consistent with prior run.
- The ~2.4× speedup is remarkably consistent across both dense and
  sparse locations — the prefilter is doing its job across the spectrum.
- This is just the first lever. The current bottleneck remaining: search
  box is still 5000m around the query because `OffsetInMeter` is set to
  5000m by publish-api; most of the per-snap work is on edges that
  pass the prefilter but turn out far from optimal. Next PR (per-tile
  max edge extent + derived search box from MaxDistance) targets that.
- Static analysis: per-snap allocation in urban-brussels dropped from
  73 MB to 52 MB. Still high — the Shape iteration churns
  IEnumerable+tuple state machines per edge. Future allocation cleanup
  (Span-based shape iteration) is a separate lever.
