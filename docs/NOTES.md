## Data

In Itinero 2 the data in the routing network is built independently from the routing profiles or any other runtime configuration. This allows the routing network to built without the need to define profiles and most aspects of the profiles can be tuned after the routing network was built.

There are 2 distinct steps:

1. Building the routing network:
    - Configured based on a general idea of the type or routing usecases not on specific profiles.
    - Attributes can be kept/dropped independent of profile implementations.
    - Attributes can existing both on edge and on vertices.
    - Turn costs can be added with their own attribute sets.
     
2. Prepare a network to use a profile:
    - A network doesn't have to be prepare for a given profile, routing will still be possible but not optimized.
    - When preparing a network for a profile two caches are built:
       - A collection of edgetypes: 
          - A set of types of edges identified by a subset of attributes by a function that maps edge attributes -> edge type attributes.
          - For each type a cache is built for each profile caching the EdgeFactor values.
       - A collection of turn cost types: 
          - A set of types of turn costs identified by a subset of their attributes. 
          - For each type a cache is built with turn costs.

## RouterDb

The data in Itinero2 is structure into a RoutingNetwork that consists of a number of NetworkTile objects. A RoutingNetwork is encapsulated in a RouterDb.

Each has it's own function:

- RoutingNetwork: A single consistent version of the routing network.  
- RouterDb: Facilitates handling a RoutingNetwork. The RouterDb is the main interface for the following functionality:  
  - Route calculations: The RouterDb is the starting point to start routing queries.
  - Writes: The RouterDb facilitates writes to the RoutingNetwork adding extra data (no mutations).
  - Updates: The RouterDb facilitates updates to the RoutingNetwork optionally mutating data.
  
## Profiles
 
A profile represents a single behaviour for a single vehicle. It has two function determining behaviour:
 
- EdgeFactor: Takes as input the attributes of an edge and determines the factor in relation to the length or an edge.
- TurnCosts: Takes as input the attributes of a potential turn and determines the turn costs.
 
 ### Profiles and RouterDb
 
RouterDb and Profiles can exist completely separate. Their responsibilities are strictly separated but in the RouterDb some facilities exists to cache some both the EdgeFactor and TurnCosts.
 
- RouterDb: Managed the data of the network. Contains geometries for each edge, the topology and attributes for each edges together with attributes on vertices.
- Profiles: Defines functions on the data of the RouterDb to determine behaviour.
 
The relation becomes more complex with the caching of EdgeFactors and TurnCosts. 
A Profile can be _registered_ on RouterDb and that RouterDb will start caching the results of EdgeFactor and TurnCosts in the data structure of the network.
 
