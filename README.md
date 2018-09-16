# routing2

**this is ongoing work on the next version of Itinero, if you want routing right now, please go [here](https://github.com/itinero/routing)**

A new temporary repository for v2 routing core, being tile-based.

This is part of the work done by the awesome [open planner team](https://openplanner.team/).

## Goals of this project

To consume routable tiles:

![Image of tiles for ghent](./docs/routable-tiles-ghent.png)

More detailed goals are:

- Experiment with tiled-routing.
- Build a fully functioning prototype of what Itinero 2 should be and:
  - Merge this with the current routing core when finished.
  - Or transplant the most valuable concepts over to the current routing core without breaking too much.
- Experiment to see if all the current features of the routing core can be implemented using Itinero 2's new concepts.
  - Island detections is one, how to do this when using multiple tiles?
  - Configurable profiles and changes to profiles?
  - Preprocessing & updating data.

## Requirements on top of what Itinero is now:

The main thing is to build a tile-based datastructure that replaces (or expands) the current routerdb concept. This should also enable distributed preprocessing of the data. It would be awesome to:

- Live load updates, meaning we can have immidate updates to the routing graph.
- We can add any data to the edges and make this configurable:
  - Distance.
  - Custom costs.
- We can add any data to the vertices and make this configurable:
  - Turn costs.
  - Intersection costs.
  - Meta data.

## Status

- [ ] Build a basic data structures in tiled form.
  - [ ] Get this done for vertices.
  - [ ] Get this done for edges.
  - [ ] Add support for turn-restrictions.
  - [ ] Load tiles on demand.
- [ ] Experment with routing algorithms on top of this.
- [ ] Build a proper resolving algorithm and verify performance.