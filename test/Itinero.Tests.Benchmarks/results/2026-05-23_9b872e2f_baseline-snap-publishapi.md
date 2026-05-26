---
date: 2026-05-23
commit: 9b872e2f
branch: develop
benchmarks: SnappingBenchmarks
runtime: .NET 10.0.0 (10.0.25.52411) Arm64 RyuJIT AdvSIMD
os: macOS 26.2 (Darwin 25.2.0)
cpu: Apple M1, 8 logical / 8 physical cores
data:
  - test/Itinero.Tests.Functional/belgium-latest.osm.routerdb (158 MB, 2026-03-19)
snap_settings: publish-api real-world — OffsetInMeter=5000, OffsetInMeterMax=5000, MaxDistance=double.MaxValue
job: out-of-process, warmupCount=2, iterationCount=4 (per-case warm only of LocationIndex's location)
label: real-world-settings baseline before snap-algorithm refactor
---

## Summary

Real-world snap baseline against the Belgium routing graph using
publish-api settings (5000 m radius, no MaxDistance cap). This is the
regime the algorithmic refactor needs to win. Setup warms only the
current LocationIndex's tile per case (out-of-process keeps each worker
clean; warming all 28 locations per worker was 5+ minutes of pure setup).

Total run: **16 min** wall-clock for 56 cases.

Key shape of the data:
- **urban-brussels (idx 0) is brutal:** 182 ms / 85 ms with **73 MB / 31 MB
  allocated per snap.** The 5 km box in Brussels covers tens of thousands
  of candidate edges; per-edge full geometry iteration + haversine + project
  blow up exactly as predicted.
- **Most Halton-spread points (idx 8–27) land in semi-urban areas** and
  cost 5–65 ms — still 100–1000× worse than what the default-settings
  baseline suggested. coast-brugge (idx 4) and the Halton points around
  Antwerp / Ghent (idx 8, 16) are the next-worst clusters at 25–65 ms.
- **Rural / motorway are cheap** even under publish-api settings:
  rural-vorselaar (idx 3) and motorway-south (idx 7) come in at ~2 ms,
  motorway-north (idx 6) at ~10 ms.
- **offshore-miss (idx 5) exits in ~700 ns** — the no-hit path is
  already fast; the refactor doesn't need to touch it.

The headline number for the refactor: **urban-brussels ToAsync_Single
must drop from 182 ms / 73 MB.** Halve that and most other points
benefit too.

## Results

```
| Method           | LocationIndex | Mean             | Error           | StdDev        | Allocated   |
|----------------- |-------------- |-----------------:|----------------:|--------------:|------------:|
| ToAsync_Single   | 0  urban-brussels        | 181,913,232 ns | 4,029,266 ns | 623,533 ns | 73013.68 KB |
| ToAllAsync_Drain | 0  urban-brussels        |  84,672,166 ns | 5,089,034 ns | 787,533 ns | 31366.38 KB |
| ToAsync_Single   | 1  suburb-zellik         |  11,301,118 ns |   243,330 ns |  37,655 ns |  3506.51 KB |
| ToAllAsync_Drain | 1  suburb-zellik         |  32,377,556 ns |   505,189 ns |  78,178 ns | 12139.26 KB |
| ToAsync_Single   | 2  village-wechelderzande |  14,225,176 ns |   231,702 ns |  35,856 ns |  4389.11 KB |
| ToAllAsync_Drain | 2  village-wechelderzande |   5,930,262 ns |   529,273 ns |  81,905 ns |  1927.32 KB |
| ToAsync_Single   | 3  rural-vorselaar       |  14,122,354 ns |   810,727 ns | 125,460 ns |  4266.66 KB |
| ToAllAsync_Drain | 3  rural-vorselaar       |   5,745,680 ns |   343,817 ns |  18,845 ns |  1984.64 KB |
| ToAsync_Single   | 4  coast-brugge          |  44,128,911 ns | 1,483,992 ns | 229,649 ns | 15460.94 KB |
| ToAllAsync_Drain | 4  coast-brugge          |  22,442,044 ns | 2,526,861 ns | 391,034 ns |   7347.9 KB |
| ToAsync_Single   | 5  offshore-miss         |          772 ns |        511 ns |       28 ns |     1.72 KB |
| ToAllAsync_Drain | 5  offshore-miss         |          642 ns |         25 ns |        3 ns |     2.06 KB |
| ToAsync_Single   | 6  motorway-north        |  10,596,533 ns |   296,882 ns |  45,942 ns |  3611.42 KB |
| ToAllAsync_Drain | 6  motorway-north        |   8,470,876 ns |   791,234 ns | 122,444 ns |   2888.1 KB |
| ToAsync_Single   | 7  motorway-south        |   2,177,434 ns |   146,469 ns |   8,028 ns |   527.31 KB |
| ToAllAsync_Drain | 7  motorway-south        |   2,221,449 ns |    90,704 ns |  14,036 ns |      698 KB |
| ToAsync_Single   | 8  halton-00 (4.31,50.92) |  65,099,439 ns | 2,257,259 ns | 123,728 ns | 23450.09 KB |
| ToAllAsync_Drain | 8  halton-00             |  30,770,172 ns | 1,192,827 ns | 184,591 ns | 12093.07 KB |
| ToAsync_Single   | 9  halton-01 (3.78,51.07) |  41,339,069 ns | 2,391,875 ns | 131,106 ns | 15341.54 KB |
| ToAllAsync_Drain | 9  halton-01             |  32,557,165 ns | 1,949,776 ns | 106,873 ns | 12349.43 KB |
| ToAsync_Single   | 10 halton-02 (4.84,50.82) |  10,242,919 ns |   105,181 ns |  16,276 ns |  2969.58 KB |
| ToAllAsync_Drain | 10 halton-02             |   4,843,123 ns |   807,764 ns | 125,002 ns |  1564.16 KB |
| ToAsync_Single   | 11 halton-03 (3.51,50.97) |  14,628,687 ns | 2,013,025 ns | 311,517 ns |  3969.45 KB |
| ToAllAsync_Drain | 11 halton-03             |   7,625,521 ns |   314,370 ns |  48,649 ns |  2052.58 KB |
| ToAsync_Single   | 12 halton-04 (4.57,51.12) |  18,427,108 ns |   632,100 ns |  97,818 ns |  5700.21 KB |
| ToAllAsync_Drain | 12 halton-04             |   9,347,681 ns | 1,728,013 ns | 267,411 ns |  3012.83 KB |
| ToAsync_Single   | 13 halton-05 (4.04,50.87) |  21,091,572 ns |   571,102 ns |  88,378 ns |  6505.96 KB |
| ToAllAsync_Drain | 13 halton-05             |  10,381,288 ns |   484,606 ns |  74,993 ns |  3386.59 KB |
| ToAsync_Single   | 14 halton-06 (5.11,51.02) |  20,885,319 ns |   302,657 ns |  16,589 ns |  6166.07 KB |
| ToAllAsync_Drain | 14 halton-06             |   9,043,736 ns |   201,554 ns |  31,190 ns |  2956.97 KB |
| ToAsync_Single   | 15 halton-07 (3.38,51.16) |  12,668,849 ns | 1,683,740 ns | 260,560 ns |  3599.54 KB |
| ToAllAsync_Drain | 15 halton-07             |   6,033,849 ns | 1,466,181 ns | 226,893 ns |  1789.55 KB |
| ToAsync_Single   | 16 halton-08 (4.44,50.79) |  59,632,472 ns |   953,726 ns | 147,590 ns | 21289.53 KB |
| ToAllAsync_Drain | 16 halton-08             |  25,826,351 ns |   900,828 ns | 139,404 ns | 10368.28 KB |
| ToAsync_Single   | 17 halton-09 (3.91,50.94) |  17,456,622 ns | 6,142,732 ns | 950,594 ns |  4654.83 KB |
| ToAllAsync_Drain | 17 halton-09             |   7,320,851 ns |   368,729 ns |  57,061 ns |  2175.13 KB |
| ToAsync_Single   | 18 halton-10 (4.97,51.08) |  17,680,426 ns |   484,207 ns |  26,541 ns |  5341.53 KB |
| ToAllAsync_Drain | 18 halton-10             |   7,849,049 ns | 1,122,130 ns | 173,651 ns |  2699.01 KB |
| ToAsync_Single   | 19 halton-11 (3.64,50.84) |  19,663,482 ns | 1,329,008 ns | 205,665 ns |  5048.18 KB |
| ToAllAsync_Drain | 19 halton-11             |   8,850,403 ns |   275,380 ns |  42,615 ns |  2713.77 KB |
| ToAsync_Single   | 20 halton-12 (4.71,50.98) |  26,796,211 ns | 3,152,021 ns | 487,778 ns |  8420.98 KB |
| ToAllAsync_Drain | 20 halton-12             |  12,185,610 ns |    12,949 ns |     709 ns |  4030.96 KB |
| ToAsync_Single   | 21 halton-13 (4.17,51.13) |  24,547,606 ns | 3,471,046 ns | 537,148 ns |  7548.88 KB |
| ToAllAsync_Drain | 21 halton-13             |  12,365,615 ns | 2,039,746 ns | 315,653 ns |  3788.19 KB |
| ToAsync_Single   | 22 halton-14 (5.24,50.89) |  13,158,249 ns |   738,640 ns |  40,487 ns |  3879.71 KB |
| ToAllAsync_Drain | 22 halton-14             |   5,717,893 ns |   361,397 ns |  55,926 ns |  1864.45 KB |
| ToAsync_Single   | 23 halton-15 (3.31,51.03) |  12,836,785 ns |   156,857 ns |  24,273 ns |  3653.57 KB |
| ToAllAsync_Drain | 23 halton-15             |   6,494,885 ns |    61,562 ns |   3,374 ns |  1890.01 KB |
| ToAsync_Single   | 24 halton-16 (4.37,51.18) |  21,381,964 ns |   337,827 ns |  52,279 ns |  7256.47 KB |
| ToAllAsync_Drain | 24 halton-16             |  28,993,892 ns |   108,777 ns |  16,833 ns | 11039.25 KB |
| ToAsync_Single   | 25 halton-17 (3.84,50.80) |  17,906,217 ns |   553,898 ns |  85,716 ns |  4939.62 KB |
| ToAllAsync_Drain | 25 halton-17             |   7,789,137 ns |   422,508 ns |  23,159 ns |  2641.73 KB |
| ToAsync_Single   | 26 halton-18 (4.91,50.95) |  18,514,495 ns | 1,739,063 ns |  95,323 ns |  5277.93 KB |
| ToAllAsync_Drain | 26 halton-18             |   8,000,346 ns |    73,199 ns |  11,327 ns |  2673.98 KB |
| ToAsync_Single   | 27 halton-19 (3.58,51.10) |  11,308,674 ns |    90,963 ns |  14,076 ns |  3077.81 KB |
| ToAllAsync_Drain | 27 halton-19             |   5,524,998 ns |   583,027 ns |  90,224 ns |  1695.19 KB |
```

## Notes

- 16 min wall-clock total; 6 min over the 10-min target. To stay under
  10 min would need either fewer locations or shorter iterations
  (warmupCount=1, iterationCount=2) — A/B comparisons would still be
  meaningful but absolute numbers noisier.
- Setup change vs prior runs: each worker only warms its own
  LocationIndex, not all 28. Cross-case caching wasn't possible
  (out-of-process, fresh worker per case) so the broader warm-up was
  pure overhead.
- An attempt at `[InProcess]` toolchain (to share setup across cases)
  hit an `IndexOutOfRangeException` in `NetworkTile.CloneForEdgeTypeMap`
  — Itinero state appears unsafe to reuse across multiple in-process
  RouterDb loads in the same process. Out-of-process is the workaround;
  fixing the underlying issue is a separate concern.
- Allocation numbers are extreme: urban-brussels `ToAsync_Single`
  allocates **73 MB per snap**. That's the GC pressure publish-api
  actually pays in production. Halving the algorithmic cost should
  also collapse allocations roughly proportionally.
