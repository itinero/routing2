---
date: YYYY-MM-DD
commit: <short-sha>
branch: <branch-name>
benchmarks: <e.g. SnappingBenchmarks, PbfBuildBenchmarks>
runtime: <e.g. .NET 10.0.0>
os: <e.g. macOS 15.2, Ubuntu 24.04>
cpu: <e.g. Apple M3 Pro, AMD Ryzen 9 7950X>
ram: <e.g. 32 GB>
data:
  - belgium-latest.osm.routerdb (size, last-modified)
  - luxembourg-latest.osm.pbf   (size, last-modified)
label: <one-line description — e.g. "baseline before bbox-prefilter">
---

## Summary

One or two sentences: what was measured, what changed since the last
recorded run, and the headline number (median latency, allocs/op, regression
or improvement). If the run is investigating a specific thing, say what.

## Results

Paste the BDN summary table here verbatim. Example shape:

```
| Method               | LocationIndex | Mean      | Error    | StdDev   | Allocated |
|--------------------- |-------------- |----------:|---------:|---------:|----------:|
| ToAsync_Single       | 0             |   123 us  |   2 us   |   1 us   |    4 KB   |
| ToAsync_Single       | 1             |   ...
| ToAllAsync_Drain     | 0             |   ...
```

If you ran multiple benchmark classes, paste each table under its own H3.

## Notes

Anything not obvious from the numbers: thermal state of the machine, other
load on the host, configuration tweaks, weird outliers, why a particular
location regressed while others stayed flat, etc.
