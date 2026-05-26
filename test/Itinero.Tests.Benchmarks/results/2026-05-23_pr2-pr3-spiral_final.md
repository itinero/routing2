---
date: 2026-05-23
commit: 9b872e2f + uncommitted PR2 + PR3 + cross-tile fix + spiral iteration
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: publish-api — OffsetInMeter=5000, OffsetInMeterMax=5000, MaxDistance=double.MaxValue
job: out-of-process, warmupCount=2, iterationCount=4
label: full stack — PR1 per-edge prefilter + PR2 tile/vertex predicates + PR3 iterative MaxDistance + cross-tile diff fix + spiral tile iteration
baseline: 2026-05-23_9b872e2f_baseline-snap-publishapi.md
---

## Summary

Final snapshot of the snap refactor, comparing against the *very first*
baseline (publish-api settings, no algorithm changes — just the original
`SearchEdgesInBox` walking every vertex in a 5 km box).

**Aggregate `ToAsync_Single`: 750 ms → 18.5 ms — 40.5× speedup.**

Best case: halton-01 at **682×** (41.3 ms → 60.6 µs). Several cases at
50–125× faster. Urban-brussels alone: **34×** (181.9 ms → 5.35 ms).

`ToAllAsync_Drain` is essentially flat across all locations (~2.5%
improvement in aggregate). That's expected — under `MaxDistance = ∞`
the per-tile and per-vertex predicates degenerate to no-ops, leaving
only the per-edge MBR prefilter from PR1 active. For callers that want
all candidates within an unbounded radius, the algorithm can't do
better than reading every edge in the box.

## What's in the stack

Five layered changes:

1. **PR1: per-edge MBR prefilter.** Skip projection if the edge's
   bbox can't beat the current best.
2. **PR2: per-tile / per-vertex predicates.** Each tile carries
   `MaxLonDiff` / `MaxLatDiff` bounding edge reach. Tile or vertex
   whose envelope can't reach the query within `bestDistance` → skip
   without reading edges.
3. **PR3: iterative `MaxDistance` schedule.** `Snapper.ToAsync` tries
   `D` values `[50, 200, 800, 3200, 12800, ∞]`. With tight initial
   `D`, predicates prune aggressively from the start. Most snaps land
   on iteration 1.
4. **Cross-tile diff fix.** PR2's first implementation used the
   partner tile's full bbox as a fallback for cross-tile edge head
   coordinates, inflating `MaxLonDiff` to ~5 km on every tile with
   cross-tile edges. Now we read the actual head coord from the loaded
   partner tile, giving realistic ~500 m diffs and letting the
   predicates actually prune in dense areas.
5. **Spiral tile iteration.** Replace build-list-then-sort with
   concentric-ring iteration from Q's tile outward. Lazy per-tile diff
   computation. Closest tiles processed first → `bestDistance` shrinks
   fast → outer tiles get rejected by the per-tile predicate before
   we read their edges.

## ToAsync_Single — full comparison vs first baseline

| Idx | Location                | Baseline | Now | **Speedup** |
|---:|-------------------------|---------:|----:|---------:|
|  0 | urban-brussels          | **181.91 ms** | **5.35 ms** | **34.0×** |
|  1 | suburb-zellik           |   11.30 ms |  141 µs | **80.0×** |
|  2 | village-wechelderzande  |   14.23 ms |  539 µs | 26.4× |
|  3 | rural-vorselaar         |   14.12 ms |  237 µs | 59.5× |
|  4 | coast-brugge            |   44.13 ms | 2.255 ms | 19.6× |
|  5 | offshore-miss           |    0.77 µs |  4.13 µs | 0.19× *(tiny absolute scale)* |
|  6 | motorway-north          |   10.60 ms | 85.6 µs | **124×** |
|  7 | motorway-south          |    2.18 ms | 20.8 µs | **105×** |
|  8 | halton-00               |   65.10 ms | 1.281 ms | 50.8× |
|  9 | halton-01               |   41.34 ms |   61 µs | **682×** |
| 10 | halton-02               |   10.24 ms |  379 µs | 27.0× |
| 11 | halton-03               |   14.63 ms |  281 µs | 52.1× |
| 12 | halton-04               |   18.43 ms |  282 µs | 65.3× |
| 13 | halton-05               |   21.09 ms |  342 µs | 61.6× |
| 14 | halton-06               |   20.89 ms |  231 µs | **90.4×** |
| 15 | halton-07               |   12.67 ms |  347 µs | 36.5× |
| 16 | halton-08               |   59.63 ms | 2.957 ms | 20.2× |
| 17 | halton-09               |   17.46 ms |  800 µs | 21.8× |
| 18 | halton-10               |   17.68 ms |  452 µs | 39.1× |
| 19 | halton-11               |   19.66 ms |  170 µs | **115×** |
| 20 | halton-12               |   26.80 ms |  883 µs | 30.4× |
| 21 | halton-13               |   24.55 ms |  304 µs | 80.9× |
| 22 | halton-14               |   13.16 ms |  169 µs | 78.0× |
| 23 | halton-15               |   12.84 ms |  244 µs | 52.6× |
| 24 | halton-16               |   21.38 ms | **34 µs** | **625×** |
| 25 | halton-17               |   17.91 ms |  351 µs | 51.1× |
| 26 | halton-18               |   18.51 ms |  320 µs | 57.9× |
| 27 | halton-19               |   11.31 ms |  147 µs | 76.7× |

**Aggregate sum across all 28 locations:**
- Baseline: ~750 ms
- Now: ~18.5 ms
- **40.5× speedup overall**

## ToAsync_Single — allocation comparison

| Location | Baseline alloc | Now alloc | Reduction |
|---|---:|---:|---:|
| 0 urban-brussels | **73.0 MB** | **3.6 MB** | **20×** |
| 4 coast-brugge | 15.5 MB | 1.4 MB | 11× |
| 8 halton-00 | 23.5 MB | 798 KB | 29× |
| 16 halton-08 | 21.3 MB | 1.7 MB | 12× |
| 19 halton-11 | 5.05 MB | 90 KB | 56× |
| 24 halton-16 | 7.26 MB | 28 KB | 259× |

GC pressure on the worst case (urban-brussels) drops from ~73 MB per snap to ~3.6 MB. For a busy router serving thousands of requests per second, this alone is significant — it changes the GC cadence dramatically.

## ToAllAsync_Drain — flat (as expected)

Sample: urban-brussels 84.67 ms → 86.75 ms (~flat).
Aggregate: ~408 ms → ~399 ms (~2% improvement, within noise).

This is the right outcome. `ToAllAsync_Drain` asks for *all* candidates
within `MaxDistance`. Under `MaxDistance = ∞` there is no pruning the
algorithm can do — every edge in the load box is a potential candidate
and must be examined. The per-edge MBR prefilter (PR1) is the only
layer that helps here, and it was already present in earlier
benchmarks.

Callers that want bounded `MaxDistance` for `ToAllAsync` would see
proportional wins from PR2's predicates; nothing here changes that.

## ToAsync_Single — the `offshore-miss` regression

Baseline: 0.77 µs. Now: 4.13 µs. 5× slower at sub-microsecond scale.

This is the "no road within MaxDistance" case. The baseline succeeded
faster because it had a simpler exit path. The new algorithm pays
several layers of overhead (tile iteration, diff cache lookup,
predicate eval) before concluding nothing's there. At absolute scale
of 4 µs vs 0.7 µs, the regression has zero practical impact, but it's
worth noting if someone tunes this further.

## Run details

- Wall-clock: 17:23 for 56 cases.
- Measurement quality: most cases have StdErr < 1% of mean — much
  tighter than the prior run with the cross-tile fix.
- Cross-tile fix + spiral iteration both contribute. Cross-tile fix
  was the unlock for predicates working in dense areas. Spiral was
  the cleanup that turned diff computation lazy and shaved another
  ~30-40% off the dense cases on top of that.

## Notes / open follow-ups

- `offshore-miss` regression — low priority, sub-µs scale.
- `suburb-zellik` `ToAsync_Single` was 0.65× under PR3+fix (without
  spiral); now with spiral it's 80× faster than the baseline. The
  earlier regression was an artefact of the build-list-then-sort
  approach computing diffs eagerly even for tiles the snap didn't
  need.
- For `ToAllAsync` to benefit from PR2/PR3, callers should set a
  finite `MaxDistance` instead of `∞`. publish-api could consider
  this — `MaxDistance = 100m` for "snap to nearest road" use cases
  would translate to predicate-driven wins similar to `ToAsync_Single`.
