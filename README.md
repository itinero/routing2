# routing2

[![.NET Core](https://github.com/itinero/routing2/workflows/.NET%20Core/badge.svg)](https://github.com/itinero/routing2/actions?query=workflow%3A%22.NET+Core%22)  
[![Test Coverage](https://www.itinero.tech/routing2/develop/badge_linecoverage.svg)](https://www.itinero.tech/routing2/develop/index.html)  

The new for the v2 routing core. This is part of the work done by the awesome [open planner team](https://openplanner.team/).

## Goals of this project

To consume routable tiles and make Itinero more flexible. Itinero should be able to update data in the routing graph and be able to handle more dynamic scenarios. Itinero should be able to do route planning using live OSM changes.

![Image of tiles for ghent](./docs/routable-tiles-ghent.png)

## Status

- [x] Build a basic data structures in tiled form.
  - [x] Get this done for vertices.
  - [x] Get this done for edges.
  - [x] Add support for turn-restrictions.
  - [x] Load tiles on demand.
- [x] Experiment with routing algorithms on top of this.
- [x] Build a proper resolving algorithm and verify performance.
