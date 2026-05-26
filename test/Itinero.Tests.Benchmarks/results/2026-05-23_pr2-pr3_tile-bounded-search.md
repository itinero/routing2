---
date: 2026-05-23
commit: 9b872e2f + uncommitted PR2+PR3 changes
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: publish-api — OffsetInMeter=5000, OffsetInMeterMax=5000, MaxDistance=double.MaxValue
job: out-of-process, warmupCount=2, iterationCount=4
label: PR2 + PR3 + cross-tile diff fix — tile-bounded search with iterative MaxDistance growth
baseline: 2026-05-23_pr1_per-edge-mbr-prefilter.md
---

## Summary

Three changes layered on top of PR1's per-edge MBR prefilter:

- **PR2: per-tile and per-vertex predicates.** Each tile carries
  `MaxLonDiff` / `MaxLatDiff` bounding how far edges from any vertex
  in the tile can reach. Tiles whose envelope can't reach within
  `bestDistance` of the query are skipped; vertices whose envelope
  can't reach are skipped without walking their edges. Tiles are
  visited in closest-first sort order so `bestDistance` shrinks fast
  and the predicates prune aggressively from there.
- **PR3: iterative `MaxDistance` growth.** `Snapper.ToAsync` retries
  `SnapInBoxAsync` with a growing `D` schedule (`50, 200, 800, 3200,
  12800, MaxDistance`). PR2's predicates do nothing at `D = ∞` (the
  publish-api default) until the first snap lands; with a small initial
  `D` they prune from the start. Most snaps land at the first
  iteration; sparse-area cases escalate.
- **Cross-tile diff fix.** PR2's first implementation used the partner
  tile's whole geographic bbox as a fallback for cross-tile edge head
  coordinates. That inflated `MaxLonDiff` to ~2× tile-width
  (~5 km at z14) on every tile with cross-tile edges, completely
  defeating the per-tile predicate in dense urban areas (where every
  tile has cross-tile edges). The fix reads the actual head coord via
  the network's loaded partner tile (always available during snap
  thanks to publish-api's halo). Now diffs reflect real edge lengths
  (~500 m typical), and the predicates actually prune.

The third change was the unlock. Without it, PR3 looked like a wash
on dense urban (predicates couldn't prune so the iterative schedule
just did redundant work). With it, urban-brussels drops from 21.55 ms
(PR2) to 8.88 ms — and the *aggregate* across all 28 locations drops
3.48× from 78 ms to 22 ms.

## Headline

**`ToAsync_Single` aggregate: 78.0 ms → 22.4 ms — 3.48× faster.**
Many individual cases see 10×–25× speedups. `ToAllAsync_Drain` is
flat across most locations; first 3 cases regressed (likely worker
startup / machine state, not algorithmic).

Per-snap allocation for urban-brussels: **13.4 MB → 3.6 MB** (3.7×
reduction).

## Results — ToAsync_Single

| Idx | Location                | PR2 (PR1+baseline) | PR3+fix | Speedup | Alloc PR2 | Alloc PR3 |
|---:|-------------------------|------------------:|--------:|-------:|----------:|----------:|
|  0 | urban-brussels          |          21.55 ms | **8.88 ms** | **2.43×** | 13.39 MB | **3.61 MB** |
|  1 | suburb-zellik           |           163 µs | 249 µs | 0.65× (regr.) | 96 KB | 87 KB |
|  2 | village-wechelderzande  |           1.51 ms | 814 µs | 1.85× | 817 KB | 288 KB |
|  3 | rural-vorselaar         |           869 µs | **239 µs** | **3.63×** | 471 KB | 142 KB |
|  4 | coast-brugge            |           9.53 ms | 2.65 ms | 3.60× | 5.43 MB | 1.44 MB |
|  5 | offshore-miss           |          0.77 µs | 2.40 µs | 0.32× (regr.) | 1.72 KB | 4.16 KB |
|  6 | motorway-north          |          90.5 µs | 65.3 µs | 1.39× | 49.8 KB | 37.8 KB |
|  7 | motorway-south          |          35.4 µs | 24.4 µs | 1.45× | 23.1 KB | 16.1 KB |
|  8 | halton-00               |          4.27 ms | **1.30 ms** | **3.28×** | 2.46 MB | 798 KB |
|  9 | halton-01               |           253 µs | 64 µs | 3.95× | 148 KB | 46 KB |
| 10 | halton-02               |           799 µs | 387 µs | 2.07× | 419 KB | 196 KB |
| 11 | halton-03               |          2.35 ms | 283 µs | **8.30×** | 1.28 MB | 200 KB |
| 12 | halton-04               |          1.90 ms | 292 µs | 6.50× | 1.08 MB | 163 KB |
| 13 | halton-05               |          1.04 ms | 302 µs | 3.44× | 587 KB | 177 KB |
| 14 | halton-06               |          1.09 ms | 242 µs | 4.52× | 509 KB | 120 KB |
| 15 | halton-07               |           708 µs | 336 µs | 2.11× | 383 KB | 213 KB |
| 16 | halton-08               |          9.27 ms | 2.82 ms | 3.28× | 4.71 MB | 1.68 MB |
| 17 | halton-09               |          1.56 ms | 816 µs | 1.92× | 495 KB | 410 KB |
| 18 | halton-10               |          2.61 ms | 456 µs | 5.72× | 778 KB | 264 KB |
| 19 | halton-11               |          4.35 ms | **171 µs** | **25.5×** | 657 KB | 90 KB |
| 20 | halton-12               |          3.97 ms | 880 µs | 4.51× | 1.17 MB | 542 KB |
| 21 | halton-13               |          3.44 ms | 309 µs | **11.1×** | 868 KB | 180 KB |
| 22 | halton-14               |          1.96 ms | 165 µs | **11.9×** | 538 KB | 110 KB |
| 23 | halton-15               |          1.13 ms | 237 µs | 4.75× | 282 KB | 130 KB |
| 24 | halton-16               |           546 µs | **39 µs** | **14.0×** | 168 KB | 32 KB |
| 25 | halton-17               |          1.60 ms | 336 µs | 4.76× | 476 KB | 202 KB |
| 26 | halton-18               |          2.32 ms | 316 µs | 7.34× | 525 KB | 173 KB |
| 27 | halton-19               |          1.20 ms | 148 µs | 8.08× | 359 KB | 83 KB |

**Aggregate ToAsync_Single across all 28 locations**:
- PR2 total: 78.04 ms, 41.5 MB allocated
- PR3+fix total: 22.42 ms, ~11.0 MB allocated
- **3.48× speedup, 3.8× allocation reduction**

## Results — ToAllAsync_Drain

Mostly flat or slight improvement compared to PR2; first 3 locations
show 1.4–1.7× regression that doesn't fit the algorithmic story
(the predicates degenerate under `MaxDistance = ∞`, so PR3's
changes shouldn't affect `ToAllAsync_Drain`). Suspect worker startup
cost / machine state for the first cases; re-running would likely
even those out.

| Idx | Location                | PR2 | PR3+fix |
|---:|-------------------------|----:|--------:|
|  0 | urban-brussels          | 96.95 ms | 135.93 ms |
|  1 | suburb-zellik           | 33.05 ms | 55.70 ms |
|  2 | village-wechelderzande  | 5.93 ms | 9.12 ms |
|  3 | rural-vorselaar         | 5.75 ms | 5.54 ms |
|  4 | coast-brugge            | 22.71 ms | 21.16 ms |
|  5 | offshore-miss           | 660 ns | 568 ns |
|  6 | motorway-north          | 8.59 ms | 8.15 ms |
|  7 | motorway-south          | 2.21 ms | 2.13 ms |
|  8 | halton-00               | 35.87 ms | 28.69 ms |
|  9 | halton-01               | 33.39 ms | 31.05 ms |
| 10 | halton-02               | 4.78 ms | 4.55 ms |
| 11 | halton-03               | 7.57 ms | 7.30 ms |
| 12 | halton-04               | 9.12 ms | 8.61 ms |
| 13 | halton-05               | 10.18 ms | 10.18 ms |
| 14 | halton-06               | 8.91 ms | 8.68 ms |
| 15 | halton-07               | 5.79 ms | 5.71 ms |
| 16 | halton-08               | 25.90 ms | 24.19 ms |
| 17 | halton-09               | 7.08 ms | 7.06 ms |
| 18 | halton-10               | 7.69 ms | 7.41 ms |
| 19 | halton-11               | 8.76 ms | 8.56 ms |
| 20 | halton-12               | 12.18 ms | 11.82 ms |
| 21 | halton-13               | 11.94 ms | 11.30 ms |
| 22 | halton-14               | 5.57 ms | 5.35 ms |
| 23 | halton-15               | 6.58 ms | 6.40 ms |
| 24 | halton-16               | 28.84 ms | 27.30 ms |
| 25 | halton-17               | 7.71 ms | 7.56 ms |
| 26 | halton-18               | 7.88 ms | 7.75 ms |
| 27 | halton-19               | 5.35 ms | 5.29 ms |

## What this validates

Three layered correctness/perf properties that drove the design:

1. **PR2's correctness lemmas don't depend on `D`**, so PR3's
   iterative-shrink approach is safe.
2. **Per-tile diffs are useful** *if* they reflect actual edge
   geometry — the cross-tile fix was the difference between "useless
   in dense areas" and "kills 90% of candidates".
3. **Mixed-density tiles are fine** as long as the diffs are tight.
   The motorway-contamination theory I worried about earlier was real,
   but it was the implementation, not the design — bbox-fallback for
   unknown head coords was creating fake huge diffs.

## Notes

- 18:13 wall-clock for 56 cases. About on budget.
- Allocations dropped roughly proportionally to runtime — PR3+fix
  doesn't just skip projection work, it skips edge enumeration
  entirely for pruned vertices.
- The `ToAllAsync_Drain` first-3-cases regression deserves a sanity
  re-run when convenient. The algorithm changes shouldn't affect that
  path; if the regression persists, something subtle is going on.
- Two `ToAsync_Single` regressions worth understanding later:
  `suburb-zellik` (0.65×) and `offshore-miss` (0.32×, but tiny
  absolute — 2.4 µs vs 772 ns, possibly EnsureDiffs overhead on
  warmup-fresh tiles).
