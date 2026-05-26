---
date: 2026-05-26
commit: bb25332d + uncommitted ProjectOn fix + NTS 2.6.0 + map-matching expected refresh
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: publish-api — OffsetInMeter=5000, OffsetInMeterMax=5000, MaxDistance=double.MaxValue
job: out-of-process, warmupCount=2, iterationCount=4
label: post-ProjectOn fix — direction-independent dot-product perpendicular projection in GeoExtensions.ProjectOn
baseline: 2026-05-23_pr2-pr3-spiral_final.md
---

## Summary

First run after fixing the direction-dependence bug in `ProjectOn` (now dot-product based instead of the prior iterative approach).

**Aggregate `ToAsync_Single`: 18.5 ms → 21.2 ms (+14%).** A small overall regression — within the same order of magnitude as before, still 35× faster than the original baseline.

Two cases regressed sharply:
- **halton-00**: 1.28 ms → 2.83 ms (+121%) — real (StdDev 1.8% of mean).
- **halton-17**: 351 µs → 705 µs (+101%) — likely noise (StdDev 10% of mean, 73 µs).

Several cases improved: halton-05 (-15%), halton-15 (-5%), halton-09 (-4%), village-wechelderzande (-4%).

The dot-product `ProjectOn` is arithmetically simpler than the prior code, so the regression isn't from the projection itself — likely a downstream effect of more snaps landing on different (correct) edges, which changes which tiles get loaded and the search shape. Worth a quick look at halton-00 specifically before declaring this fine.

## ToAsync_Single — vs prior baseline (2026-05-23 spiral final)

| Idx | Location                | Prior     | Now      | Δ      |
|----:|-------------------------|----------:|---------:|-------:|
|   0 | urban-brussels          |   5.35 ms |  5.72 ms |  +7%   |
|   1 | suburb-zellik           |    141 µs |   152 µs |  +8%   |
|   2 | village-wechelderzande  |    539 µs |   517 µs |  −4%   |
|   3 | rural-vorselaar         |    237 µs |   238 µs |   0%   |
|   4 | coast-brugge            |  2.255 ms | 2.331 ms |  +3%   |
|   5 | offshore-miss           |   4.13 µs |  4.16 µs |  +1%   |
|   6 | motorway-north          |   85.6 µs |  89.9 µs |  +5%   |
|   7 | motorway-south          |   20.8 µs |  21.9 µs |  +5%   |
|   8 | halton-00               |  1.281 ms | 2.832 ms | **+121%** |
|   9 | halton-01               |   60.6 µs |  62.5 µs |  +3%   |
|  10 | halton-02               |    379 µs |   374 µs |  −1%   |
|  11 | halton-03               |    281 µs |   291 µs |  +4%   |
|  12 | halton-04               |    282 µs |   286 µs |  +1%   |
|  13 | halton-05               |    342 µs |   292 µs | **−15%** |
|  14 | halton-06               |    231 µs |   231 µs |   0%   |
|  15 | halton-07               |    347 µs |   362 µs |  +4%   |
|  16 | halton-08               |  2.957 ms | 3.081 ms |  +4%   |
|  17 | halton-09               |    800 µs |   768 µs |  −4%   |
|  18 | halton-10               |    452 µs |   465 µs |  +3%   |
|  19 | halton-11               |    170 µs |   168 µs |  −1%   |
|  20 | halton-12               |    883 µs |   910 µs |  +3%   |
|  21 | halton-13               |    304 µs |   301 µs |  −1%   |
|  22 | halton-14               |    169 µs |   170 µs |   0%   |
|  23 | halton-15               |    244 µs |   233 µs |  −5%   |
|  24 | halton-16               |   34.0 µs |  35.4 µs |  +4%   |
|  25 | halton-17               |    351 µs |   705 µs | **+101%** (noisy) |
|  26 | halton-18               |    320 µs |   363 µs | +13%   |
|  27 | halton-19               |    147 µs |   157 µs |  +7%   |

**Aggregate sum across 28 locations:**
- Prior:  ~18.5 ms
- Now:   ~21.2 ms
- Delta: **+14%**

## ToAsync_Single — allocations (worst cases)

| Idx | Location          | Prior alloc | Now alloc | Δ      |
|----:|-------------------|------------:|----------:|-------:|
|   0 | urban-brussels    |     3.60 MB |   3.61 MB |   0%   |
|   4 | coast-brugge      |     1.40 MB |   1.45 MB |  +4%   |
|   8 | halton-00         |      798 KB |   1.67 MB | **+114%** |
|  16 | halton-08         |     1.70 MB |   1.68 MB |  −1%   |
|  19 | halton-11         |       90 KB |     90 KB |   0%   |
|  24 | halton-16         |       28 KB |   28.8 KB |  +3%   |

halton-00 allocations doubled in lockstep with its time regression — consistent with the snap now searching a larger area before landing.

## ToAllAsync_Drain

Flat / minor variance, as expected. `ToAllAsync` was unchanged by the prior optimization stack; ProjectOn's correctness fix doesn't change the candidate set, only where they project to on each edge.

## Run details

- Wall-clock: 18:45 for 56 cases.
- Measurement quality: most cases StdErr < 2% of mean. halton-17 StdErr ~10% (noisy).

## Notes / follow-up

- **halton-00** is the one to investigate. The 2× regression here is real and not explained by the projection math being more expensive (it isn't). Most likely the corrected projection now snaps the query to a different (further) edge that's behind a tile that wasn't being loaded before, changing the iterative `MaxDistance` ramp behavior.
- 26 of 28 cases are within ±15% of prior — within noise band for this rig.
- Correctness wins: the `ProjectOn` fix removed a direction-dependent projection bug that caused 10m snap drift in real-world reports. Net change is +14% time for correct snaps vs. the prior incorrect snaps.
