# routing2

**this is ongoing work on the next version of Itinero, if you want routing right now, please go [here](https://github.com/itinero/routing)**

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:anyways_Openplannerteam_ItineroTiledRouting)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=anyways_Openplannerteam_ItineroTiledRouting)  

The new for the v2 routing core. This is part of the work done by the awesome [open planner team](https://openplanner.team/).

## Goals of this project

To consume routable tiles and make Itinero more flexible. Itinero should be able to update data in the routing graph and be able to handle more dynamic scenarios. Itinero should be able to do route planning using live OSM changes.

![Image of tiles for ghent](./docs/routable-tiles-ghent.png)

## Requirements on top of what Itinero is now:

The main thing is to build a tile-based datastructure that replaces (or expands) the current routerdb concept. This should also enable distributed preprocessing of the data. It would be awesome to:

- [x] Have the route planner load data automatically from [Routeable Tiles](https://github.com/openplannerteam/routable-tiles).
- [ ] Live load updates, meaning we can have immediate updates to the routing graph.
  - [ ] Keep multiple version in memory at the same time.
- [ ] We can precompute edge data:
  - [ ] Update this on-the-fly when not there.
  - [ ] Experiment with:
    - [ ] Dynamic travel times.
- [ ] We can precompute vertex data:
  - [ ] Update this on-the-fly when not there.
  - [ ] Experiment with:
    - [ ] Turn costs.
    - [ ] Intersection costs.

## Status

- [x] Build a basic data structures in tiled form.
  - [x] Get this done for vertices.
  - [x] Get this done for edges.
  - [ ] Add support for turn-restrictions.
  - [x] Load tiles on demand.
- [x] Experiment with routing algorithms on top of this.
- [x] Build a proper resolving algorithm and verify performance.
