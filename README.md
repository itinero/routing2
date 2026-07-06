# Itinero 2

[![.NET Core](https://github.com/itinero/routing2/workflows/.NET%20Core/badge.svg)](https://github.com/itinero/routing2/actions?query=workflow%3A%22.NET+Core%22)  
[![Test Coverage](https://www.itinero.tech/routing2/develop/badge_linecoverage.svg)](https://www.itinero.tech/routing2/develop/index.html)  

Itinero 2 is a ground-up rebuild of the Itinero routing core, designed with **extreme flexibility as its guiding principle**. Both routing profiles and the underlying network data are treated as first-class, mutable inputs: profiles are evaluated at query time rather than baked into the graph, and the network itself can be updated, extended, or partially reloaded without a full rebuild.

This flexibility is a deliberate trade-off. Itinero 2 does **not** implement heavyweight speed-up techniques such as contraction hierarchies, customizable contraction hierarchies, or hub labels. The routing algorithms are plain **Dijkstra** and **A***. Anything that pre-commits the graph to a fixed set of profiles or a fixed cost model has been left out on purpose, it would trade away the properties that make this library useful for the scenarios it targets (dynamic edits, live OSM changes, per-request profile tuning, etc.).

To keep query performance reasonable without giving up that flexibility, Itinero 2 uses a **tile-based network representation**. Tiles let us prebuild and cache the parts of the graph that are cheap to prebuild (geometry, topology, spatial indexes) and load them on demand, while leaving the profile-dependent parts (edge cost, access, turn cost) to be computed per query. The result sits between a fully dynamic in-memory graph and a fully preprocessed one.

![Image of tiles for ghent](./docs/routable-tiles-ghent.png)

## Features

- **Tile-based routing network** with on-demand loading and support for in-place edits (`Itinero`).
- **Plain Dijkstra and A*** routing, no contraction hierarchies or other preprocessed speed-ups.
- **Query-time profiles**, so cost models can be swapped or tuned per request. Profiles can be written in **C#** (the recommended approach) or in **Lua** via `Itinero.Profiles.Lua`.
- **OSM import** directly into the tiled network format (`Itinero.IO.Osm`), or via an intermediate format where tiles are written to disk and loaded on demand.
- **Map matching** for GPS traces using a Hidden Markov Model / Viterbi approach (`Itinero.MapMatching`).
- **Turn-by-turn instructions** generation (`Itinero.Instructions`).
- **Geospatial helpers** for coordinates, projections and distance math (`Itinero.Geo`).

## References & Acknowledgements

Itinero 2 started as part of the work done by the [open planner team](https://openplanner.team/) and feature are driven and funded by [ANYWAYS](https://www.anyways.eu/).

- **Colpaert, P., Abelshausen, B., Rojas Meléndez, J., Delva, H. & Verborgh, R. (2019).** *Republishing Open Street Map's roads as Linked Routable Tiles.* Proceedings of the 16th ESWC: Posters and Demos, Springer, vol. 11762, pp. 13–17. https://link.springer.com/chapter/10.1007/978-3-030-32327-1_3

The `Itinero.MapMatching` package implements Hidden Markov Model (HMM) based map matching, based on:

- **Newson, P. & Krumm, J. (2009).** *Hidden Markov Map Matching Through Noise and Sparseness.* Proceedings of the 17th ACM SIGSPATIAL International Conference on Advances in Geographic Information Systems. https://doi.org/10.1145/1653771.1653818

Parameter tuning, filtering strategies, and overall map matching design were informed by these open-source implementations:

- **[GraphHopper](https://github.com/graphhopper/graphhopper)** (Java, Apache-2.0) — The `map_matching` module implements Newson & Krumm with Viterbi decoding. GraphHopper's approach to candidate search, sigma_z tuning, and beta parameter selection informed our defaults.
- **[Valhalla](https://github.com/valhalla/valhalla)** (C++, MIT) — Valhalla's `meili` map matching engine provided reference values for close-point filtering (MinPointDistance), breakage distance thresholds, and transition cost parameters.
- **[OSRM](https://github.com/Project-OSRM/osrm-backend)** (C++, BSD-2-Clause) — The OSRM matching plugin's handling of GPS noise parameters and search radius selection served as an additional reference point for emission probability calibration.
