# Data Structures

## Graph

The graph data is structured per tile to ensure it is mutable, we define a fixed zoom level, default 14, when constructing the graph. 

We can add or remove vertices without the need to:
 - Change the IDs of unrelated vertices.
 - Sort the entire graph again (like when using a Hilbert Curve)
 
We should be able to update the graph in a way that allows multiple simultaneous reads and one write. A graph that is being routed on should always stay in a consistent state, after a write succeeds a new graph is made. 
 
### Vertices
 
The entry point of a graph is its vertices. A vertex ID consists of two parts:
- A tile ID: this is a global unique ID representing one tile at a predefined zoom level.
- A local ID: an id local to the tile.

### Edges

The graph is a **multigraph**, or in other words, multiple edges between the same two vertices are allowed. 

Edges are also stored per tile, they can also be requested by their ID. An edge ID consists of two parts:
- A tile ID: this is a global unique ID representing one tile.
- A local ID: an id local to the tile.  

Some edges will belong to two tiles, one for their first vertex and one for their second. They will have **two IDs** for simplicity. If one tile is removed one of the edges will be kept. This also means that edges that overlap edge boundaries will be stored twice.

### TODO: how do we store shapes.  

We definitely want to:
- store them per edge, offset by their tile again.
- we probably also want to diff-encode them.

### TODO: how do we store attributes 

We probably want to:
- store them also per tile so we can remove them again like all the other stuff.

This could become problematic because it would lead to each tile storing the same strings over and over again. We need to experiment with this a bit more.

### TODO: Data

For routing we need to store data on edges at least. Best is we also support storing data on vertices.

There are some open questions:
- Do we store weights per profile or some abstract metrics?
- Do we allow for dynamic metrics or some other stuff to be stored?

This also has to do with how we design the profiles.