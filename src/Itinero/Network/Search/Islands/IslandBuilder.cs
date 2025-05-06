using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.DataStructures;

namespace Itinero.Network.Search.Islands;

internal class IslandBuilder
{
    public static async Task BuildForTileAsync(RoutingNetwork network, Profile profile, uint tileId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        var islands = network.IslandManager.GetIslandsFor(profile);

        var tile = network.GetTileForRead(tileId);
        if (tile == null) return;

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(tile);

        var labels = new IslandLabels(network.IslandManager.MaxIslandSize);
        var costFunction = network.GetCostFunctionFor(profile);
        var vertex = new VertexId(tileId, 0);
        while (enumerator.MoveTo(vertex))
        {
            while (enumerator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested) return;

                var onIsland = await IsOnIslandAsync(network, labels, costFunction, enumerator.EdgeId,
                    IsOnIslandAlready, cancellationToken);
                if (onIsland == null) continue; // edge cannot be traversed by profile.
                if (!onIsland.Value) continue; // edge is not on island.

                islands.SetEdgeOnIsland(enumerator.EdgeId);
            }

            vertex = new VertexId(tileId, vertex.LocalId + 1);
        }

        if (cancellationToken.IsCancellationRequested) return;
        islands.SetTileDone(tileId);
        return;

        bool? IsOnIslandAlready(IEdgeEnumerator e)
        {
            return islands.IsEdgeOnIsland(e, tileId);
        }
    }

    internal static async Task<bool?> IsOnIslandAsync(RoutingNetwork network, IslandLabels labels,
        ICostFunction costFunction, EdgeId edgeId,
        Func<IEdgeEnumerator, bool?>? isOnIslandAlready = null, CancellationToken cancellationToken = default)
    {
        // to check if an edge is on an island we do the following:
        // - verify it can be used as an origin by searching forward and finding a big connected island.
        // - verify it can be used as a destination by searching backward and finding a big connected island.

        // the following assumptions are used:
        // - two bidirectional edges that share a vertex are always on the same island.
        // - an island bigger than a given threshold is considered connected.
        // - onedirectional edges are always their own islands until they occur in a loop.

        var edgeEnumerator = network.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(edgeId, true);
        var costEnumerator = network.GetEdgeEnumerator();
        var canForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        costEnumerator.MoveTo(edgeId, false);
        var canBackward = costFunction.GetIslandBuilderCost(costEnumerator);

        // test if the edge can be traversed, if not return undefined value.
        if (!canForward && !canBackward) return null;

        // see if the edge already has a label.
        if (!labels.TryGetWithDetails(edgeId, out var rootIsland))
        {
            // check if the neighbour has a status already we can use.
            var onIslandAlready = isOnIslandAlready?.Invoke(edgeEnumerator);
            if (onIslandAlready != null)
            {
                if (onIslandAlready.Value)
                {
                    rootIsland = labels.AddNew(edgeId, false);
                }
                else
                {
                    rootIsland = labels.AddTo(IslandLabels.NotAnIslandLabel, edgeId);
                }
            }
            else
            {
                // create the root island.
                rootIsland = labels.AddNew(edgeId);
            }
        }

        if (rootIsland.size >= network.IslandManager.MaxIslandSize)
        {
            if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                throw new Exception("A large island without the not-an-island label should not exist");
            return false;
        }

        if (rootIsland.final) return true; // the island can never ever get bigger anymore.

        // a search in two parts:
        // - a search space of routes going towards the edge, the destination heap.
        // - a search space of routes going away from the edge, the origin heap.
        // when either of the searches stops before the island is final the edge is on an island.

        var originHeap = new BinaryHeap<(EdgeId id, bool forward)>();
        var originVisits = new HashSet<(EdgeId id, bool forward)>();
        var originHasNonIsland = false;
        var destinationHeap = new BinaryHeap<(EdgeId id, bool forward)>();
        var destinationVisits = new HashSet<(EdgeId id, bool forward)>();
        var destinationHasNonIsland = false;
        if (canForward)
        {
            originHeap.Push((edgeId, true), 1);
            destinationHeap.Push((edgeId, true), 1);
        }
        if (canBackward)
        {
            originHeap.Push((edgeId, false), 1);
            destinationHeap.Push((edgeId, false), 1);
        }

        while (originHeap.Count > 0 || destinationHeap.Count > 0)
        {
            if (originVisits.Count >= (network.IslandManager.MaxIslandSize * 64) ||
                destinationVisits.Count >= (network.IslandManager.MaxIslandSize * 64))
            {
                break;
            }

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return null;

                if (destinationHeap.Count == 0) break;
                var currentEdge = destinationHeap.Pop(out var hops);
                if (!destinationVisits.Add(currentEdge)) continue;

                if (!edgeEnumerator.MoveTo(currentEdge.id, currentEdge.forward))
                    throw new Exception("Enumeration attempted to an edge that does not exist");
#if DEBUG
                // TODO: if we queue the tail vertex then we can avoid this move.
                var currentCanMove = costFunction.GetIslandBuilderCost(edgeEnumerator);
                if (!currentCanMove)
                    throw new Exception(
                        "Queued edge always has to be traversable in the opposite queued direction in towards search");
#endif

                // enumerate the neighbours at the tail and propagate labels if a 
                // move is possible neighbour -> tail -> current.
                // TODO: queue previous edges too to be able to handle more complex restrictions.
                var previousEdges =
                    new (EdgeId edgeId, byte? turn)[] { (edgeEnumerator.EdgeId, edgeEnumerator.TailOrder) };

                // notify usage of vertex before loading neighbours.
                await network.UsageNotifier.NotifyVertex(network, edgeEnumerator.Tail, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return null;

                // enumerate the neighbours and see if the label propagates.
                if (!edgeEnumerator.MoveTo(edgeEnumerator.Tail)) throw new Exception("Vertex does not exist");
                while (edgeEnumerator.MoveNext())
                {
                    if (edgeEnumerator.EdgeId == currentEdge.id) continue;

                    // see the turn neighbour -> tail -> current is possible.
                    // this means we need to check the neighbour in it's current backward direction and current in forward.
                    var canMove = costFunction.GetIslandBuilderCost(edgeEnumerator, false, previousEdges);
                    if (!canMove) continue;

                    // get label for the current edge, we get it here because it could have changed during neighbour processing.
                    if (!labels.TryGetWithDetails(currentEdge.id, out var currentLabelDetails))
                        throw new Exception("Current should already have been assigned a label");

                    // get neighbour label, if it exists already.
                    var neighbourLabelDetails = labels.GetOrCreateLabel(edgeEnumerator, isOnIslandAlready);

                    // a connection can be made, the path neighbour -> current is possible.
                    var madeConnection = labels.ConnectTo(neighbourLabelDetails.label, currentLabelDetails.label);
                    if (!madeConnection)
                    {
                        // check if there is a bidirectional link.
                        var canMoveOtherDirection =
                            costFunction.GetIslandBuilderCost(edgeEnumerator, true, previousEdges);
                        if (canMoveOtherDirection)
                        {
                            labels.ConnectTo(currentLabelDetails.label, neighbourLabelDetails.label);
                            madeConnection = true;
                        }
                    }

                    if (madeConnection)
                    {
                        // check root island again, it could have grown.
                        if (labels.TryGetWithDetails(edgeId, out rootIsland))
                        {
                            if (rootIsland.size >= network.IslandManager.MaxIslandSize)
                            {
                                if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                                    throw new Exception(
                                        "A large island without the not-an-island label should not exist");
                                return false;
                            }

                            if (rootIsland.final)
                                return true; // the island can never ever get bigger anymore.
                        }

                        // if not, get the neighbour label again, it is now different.
                        if (!labels.TryGetWithDetails(edgeEnumerator.EdgeId, out neighbourLabelDetails))
                            throw new Exception("This should always exist at this point");
                    }

                    if (neighbourLabelDetails.final)
                    {
                        originHasNonIsland = originHasNonIsland ||
                                             neighbourLabelDetails.label == IslandLabels.NotAnIslandLabel;

                        // if the neighbour already has a final label there are only two possibilities here:
                        // - this neighbour is an island, a connection with a non-island will never be made, no need to search further.
                        // - this neighbour is not an island, a connection with a non-island edge was made, no need to search further.
                        continue;
                    }

                    // add the neighbour to the queue, but in the opposite direction as currently enumerated.
                    var neighbourHops = hops + 1;
                    destinationHeap.Push((edgeEnumerator.EdgeId, !edgeEnumerator.Forward),
                        neighbourHops);
                }

                break;
            }

            // do away.
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return null;

                if (originHeap.Count == 0) break;
                var currentEdge = originHeap.Pop(out var hops);
                if (!originVisits.Add(currentEdge)) continue;

                if (!edgeEnumerator.MoveTo(currentEdge.id, currentEdge.forward))
                    throw new Exception("Enumeration attempted to an edge that does not exist");
#if DEBUG
                // TODO: if we queue the tail vertex then we can avoid this move.
                var currentCanMove = costFunction.GetIslandBuilderCost(edgeEnumerator);
                if (!currentCanMove)
                    throw new Exception(
                        "Queued edge always has to be traversable in the opposite queued direction in towards search");
#endif

                // enumerate the neighbours at the tail and propagate labels if a 
                // move is possible current -> head -> neighbour.
                // TODO: queue previous edges too to be able to handle more complex restrictions.
                var previousEdges =
                    new (EdgeId edgeId, byte? turn)[] { (edgeEnumerator.EdgeId, edgeEnumerator.HeadOrder) };

                // notify usage of vertex before loading neighbours.
                await network.UsageNotifier.NotifyVertex(network, edgeEnumerator.Head, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return null;

                // enumerate the neighbours and see if the label propagates.
                if (!edgeEnumerator.MoveTo(edgeEnumerator.Head)) throw new Exception("Vertex does not exist");
                while (edgeEnumerator.MoveNext())
                {
                    if (edgeEnumerator.EdgeId == currentEdge.id) continue;

                    // see the turn current -> head -> neighbour is possible.
                    var canMove = costFunction.GetIslandBuilderCost(edgeEnumerator, true, previousEdges);
                    if (!canMove) continue;

                    // get label for the current edge, we get it here because it could have changed during neighbour processing.
                    if (!labels.TryGetWithDetails(currentEdge.id, out var currentLabelDetails))
                        throw new Exception("Current should already have been assigned a label");

                    // get neighbour label, if it exists already.
                    var neighbourLabelDetails = labels.GetOrCreateLabel(edgeEnumerator, isOnIslandAlready);

                    // a connection can be made, the path current -> neighbour is possible.
                    var madeConnection = labels.ConnectTo(currentLabelDetails.label, neighbourLabelDetails.label);
                    if (!madeConnection)
                    {
                        // check if there is a bidirectional link.
                        var canMoveOtherDirection =
                            costFunction.GetIslandBuilderCost(edgeEnumerator, false, previousEdges);
                        if (canMoveOtherDirection)
                        {
                            labels.ConnectTo(neighbourLabelDetails.label, currentLabelDetails.label);
                            madeConnection = true;
                        }
                    }

                    if (madeConnection)
                    {
                        // check root island again, it could have grown.
                        if (labels.TryGetWithDetails(edgeId, out rootIsland))
                        {
                            if (rootIsland.size >= network.IslandManager.MaxIslandSize)
                            {
                                if (rootIsland.label != IslandLabels.NotAnIslandLabel)
                                    throw new Exception(
                                        "A large island without the not-an-island label should not exist");
                                return false;
                            }

                            if (rootIsland.final)
                                return true; // the island can never ever get bigger anymore.
                        }

                        // if not, get the neighbour label again, it is now different.
                        if (!labels.TryGetWithDetails(edgeEnumerator.EdgeId, out neighbourLabelDetails))
                            throw new Exception("This should always exist at this point");
                    }

                    if (neighbourLabelDetails.final)
                    {
                        destinationHasNonIsland = destinationHasNonIsland ||
                                                  neighbourLabelDetails.label == IslandLabels.NotAnIslandLabel;

                        // if the neighbour already has a final label there are only two possibilities here:
                        // - this neighbour is an island, a connection with a non-island will never be made, no need to search further.
                        // - this neighbour is not an island, a connection with a non-island edge was made, no need to search further.
                        continue;
                    }

                    // add the neighbour to the queue.
                    var neighbourHops = hops + 1;
                    originHeap.Push((edgeEnumerator.EdgeId, edgeEnumerator.Forward),
                        neighbourHops);
                }

                break;
            }
        }

        if (!labels.TryGetWithDetails(edgeId, out rootIsland)) throw new Exception("Root should have an island here");
        if (rootIsland.size >= network.IslandManager.MaxIslandSize)
            throw new Exception("This should have been tested before");
        if (rootIsland.final) throw new Exception("This should have been tested before");

        // island cannot get bigger anymore.
        // it has also not reached the min size.
        labels.SetAsFinal(rootIsland.label);
        return true;
    }

    // private static async Task WriteGeoJson(RoutingNetwork network, IslandLabels labels, EdgeId edge)
    // {
    //     await File.WriteAllTextAsync($"island_labels_{edge.LocalId}_{edge.TileId}_{(long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds}.geojson",
    //         await labels.ToGeoJson(network));
    // }
}
