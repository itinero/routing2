# routing2

A new temporary repository for v2 routing core, being tile-based.

## Goals of this project

- Experiment with tiled-routing.
- Build a prototype of what Itinero 2 should look like.
- Experiment to see if all the current features can be implement in Itinero 2's new concepts.
  - Island detections is one, how to do this when using multiple tiles?
  - Profiles and changes to profiles?
  - 

## Requirements on top of what Itinero is now:

The main thing is tiled-routing and thus distributed preprocessing. It would be awesome to:

- Live load updates, meaning we can have immidate updates to the routing graph.
  - Even more awesome if we could do this contracted but for now we should only allow this on uncontracted graphs.
- We can add any data to the edges:
  - Distance.
  - Custom costs.
  - 

## Basic concepts

## Status

- [ ] Built a basic data structures in tiled form.
- [ ] Experment with routing algorithms on top of this.
- [ ] Build a proper resolving algorithm and verify performance.