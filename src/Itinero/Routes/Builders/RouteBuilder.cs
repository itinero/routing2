using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routes.Paths;

namespace Itinero.Routes.Builders;

/// <summary>
///     Default route builder implementation.
/// </summary>
public class RouteBuilder : IRouteBuilder
{
    private readonly Func<IEnumerable<(string, string)>, bool, double, RoutingNetworkEdgeEnumerator, IEnumerable<(string, string)>>? _calculateExtraAttributes;
    private static readonly ThreadLocal<RouteBuilder> DefaultLazy = new(() => new RouteBuilder());

    /// <summary>
    ///     Gets the default instance (for the local thread).
    /// </summary>
    public static RouteBuilder Default => DefaultLazy.Value;

    /// <summary>
    /// </summary>
    /// <param name="calculateExtraAttributes">
    /// If this callback is given, it will be executed on every segment to inject extra attributes into the ShapeMeta.
    /// The parameters are:
    /// - attributes:be a reference to the attributes which the edgeEnumerator gave
    /// - direction: if we are travelling in forward direction over the edge
    /// - distance: the length we are travelling on the edge
    /// - edgeEnumerator: the current edgeEnumerator
    /// </param>
    public RouteBuilder(Func<IEnumerable<(string, string)>, bool, double, RoutingNetworkEdgeEnumerator, IEnumerable<(string, string)>>? calculateExtraAttributes = null)
    {
        _calculateExtraAttributes = calculateExtraAttributes;
    }

    /// <inheritdoc />
    public Result<Route> Build(RoutingNetwork routingNetwork, Profile profile, Path path)
    {
        var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
        var profileCached = routingNetwork.GetCachedProfile(profile);

        // SpareEnumerator is used in the branch-calculation. IT offers no guarantees about the state
        var spareEnumerator = routingNetwork.GetEdgeEnumerator();
        var route = new Route { Profile = profile.Name };
        var branches = new List<Route.Branch>();
        var seenEdges = 0;

        var allEdgeIds = new HashSet<EdgeId>();
        foreach (var edge in path)
        {
            allEdgeIds.Add(edge.edge);
        }

        foreach (var (edge, direction, offset1, offset2) in path)
        {
            if (route.Shape.Count == 0)
            {
                // This is the first edge of the route - we have to check for branches at the start loction
                bool firstEdgeIsFullyContained;
                if (direction)
                {
                    // Forward: we look to offset1
                    firstEdgeIsFullyContained = offset1 == 0;
                }
                else
                {
                    firstEdgeIsFullyContained = offset2 == ushort.MaxValue;
                }

                if (firstEdgeIsFullyContained)
                {
                    // We check for branches
                    edgeEnumerator.MoveTo(edge, direction);
                    this.AddBranches(edgeEnumerator.Tail, edgeEnumerator, spareEnumerator, 0, branches, allEdgeIds);
                }
            }

            edgeEnumerator.MoveTo(edge, direction);

            var attributes = edgeEnumerator.Attributes;
            var factor = profileCached.Factor(edgeEnumerator);
            var distance = edgeEnumerator.EdgeLength();
            distance = (offset2 - offset1) / (double)ushort.MaxValue * distance;
            route.TotalDistance += distance;

            if (_calculateExtraAttributes != null)
            {
                attributes = attributes.Concat(_calculateExtraAttributes(attributes, direction, distance, edgeEnumerator));
            }
            var speed = direction ? factor.ForwardSpeedMeterPerSecond : factor.BackwardSpeedMeterPerSecond;
            if (speed <= 0)
            {
                speed = 10;
                // Something is wrong here
                // throw new ArgumentException("Speed is zero! Route is not possible with the given profile.");
            }

            var time = distance / speed;
            route.TotalTime += time;


            // add shape points to route.
            using var shapeBetween = edgeEnumerator.GetShapeBetween(offset1, offset2).GetEnumerator();
            // skip first coordinate if there are already previous edges.
            if (route.Shape.Count > 0 && offset1 == 0)
            {
                shapeBetween.MoveNext();
            }


            while (shapeBetween.MoveNext())
            {
                route.Shape.Add(shapeBetween.Current);
            }

            if (offset1 != 0 ||
                offset2 != ushort.MaxValue)
            {
                attributes = attributes.Concat(new (string key, string value)[]
                {
                        ("_segment_offset1",
                            (offset1 / (double)ushort.MaxValue).ToString(System.Globalization.CultureInfo
                                .InvariantCulture)),
                        ("_segment_offset2",
                            (offset2 / (double)ushort.MaxValue).ToString(System.Globalization.CultureInfo
                                .InvariantCulture)),
                });
            }

            attributes = attributes.Concat(new (string key, string value)[]
            {
                    ("_segment_forward", direction.ToString())
            });

            route.ShapeMeta.Add(new Route.Meta
            {
                Shape = route.Shape.Count - 1,
                Attributes = attributes,
                AttributesAreForward = direction,
                Distance = distance,
                Profile = profile.Name,
                Time = time
            });

            // Intermediate points of an edge will never have branches
            // So, to calculate branches, it is enough to only look at the last point of the edge
            // (and the first point of the first edge - a true edge case)
            // (Also note that the first and last edge might not be needed entirely, so that means we possible can ignore those branches)

            // What is the end vertex? Add its branches...
            if (seenEdges + 1 == path.Count)
            {
                // Hmm, this is the last edge
                // We should add the branches of it, but only if the edge is completely contained
                bool lastEdgeIsFullyContained;
                if (!direction)
                {
                    // Backward: we look to offset1
                    lastEdgeIsFullyContained = offset1 == 0;
                }
                else
                {
                    lastEdgeIsFullyContained = offset2 == ushort.MaxValue;
                }

                if (lastEdgeIsFullyContained)
                {
                    this.AddBranches(edgeEnumerator.Head,
                        edgeEnumerator, spareEnumerator, route.Shape.Count - 1, branches, allEdgeIds);
                }
            }
            else
            {
                this.AddBranches(edgeEnumerator.Head, edgeEnumerator, spareEnumerator, route.Shape.Count - 1, branches,
                    allEdgeIds);
            }

            seenEdges++;
        }

        route.Branches = branches.ToArray();

        return route;
    }

    /// <summary>
    ///     Calculate all the branches of 'centralVertexid' 
    /// </summary>
    /// <param name="edgeEnumerator">The edge enumerator, moved to the point of which the branches should be added</param>
    /// <param name="spareEnumerator">An extra enumerator to use. Will be moved during the call</param>
    /// <param name="centralVertexId">The vertex id of the point under consideration</param>
    /// <param name="shapeIndex">The index of the shape of the current vertex</param>
    /// <param name="addTo">All the branches are collected into this collection</param>
    /// <param name="branchBlacklist">Edges in this list won't be used as branches</param>
    private void AddBranches(VertexId centralVertexId, RoutingNetworkEdgeEnumerator edgeEnumerator,
        RoutingNetworkEdgeEnumerator spareEnumerator, int shapeIndex,
        ICollection<Route.Branch> addTo, HashSet<EdgeId> branchBlacklist)
    {
        edgeEnumerator.MoveTo(centralVertexId);
        while (edgeEnumerator.MoveNext())
        {
            // Iterates over all edges of the endvertex

            if (branchBlacklist.Contains(edgeEnumerator.EdgeId))
            {
                // We make sure not to pick the current nor the next edge of the path
                // This is simple as we have a hashset containing _all_ the edge ids ¯\_(ツ)_/¯
                continue;
            }

            // If the vertex of the crossroads are the same as the from, we would walk forward over the branch if we would take it
            var isForward = edgeEnumerator.Forward;
            spareEnumerator.MoveTo(edgeEnumerator.EdgeId, isForward);
            using var shapeEnum = spareEnumerator.GetShapeBetween(includeVertices: false).GetEnumerator();
            shapeEnum.MoveNext(); // Init enumerator at first value
            shapeEnum.MoveNext();
            var branch = new Route.Branch
            {
                Attributes = edgeEnumerator.Attributes,
                Shape = shapeIndex,
                AttributesAreForward = isForward,
                Coordinate = shapeEnum.Current
            };
            addTo.Add(branch);
        }
    }
}
