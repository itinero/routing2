using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using OsmSharp;

namespace Itinero.IO.Osm.Restrictions;

public static class OsmTurnRestrictionExtensions
{
    /// <summary>
    /// The signature of a function to get edges for the given nodes pair along the given way.
    /// </summary>
    public delegate (EdgeId edge, bool forward)? GetEdgeFor(long wayId, int node1Idx, int node2Idx);
    
    /// <summary>
    /// Converts the given OSM turn restriction into one or more sequences on the network.
    /// </summary>
    /// <param name="osmTurnRestriction">The OSM turn restriction.</param>
    /// <param name="getEdgeFor">A function to get edges for pairs of nodes for a given way.</param>
    /// <returns>The restriction using network edges and vertices.</returns>
    public static Result<IEnumerable<NetworkTurnRestriction>> ToNetworkRestrictions(
        this OsmTurnRestriction osmTurnRestriction,
        GetEdgeFor getEdgeFor)
    {
        // get from edges.
        var fromEdges = new List<(EdgeId edge, bool forward)>();
        foreach (var hop in osmTurnRestriction.GetFromHops())
        {
            if (hop.IsError) continue;
            var (way, minStartNode, endNode) = hop.Value;

            // get the from-hop from the two nodes and the way id.
            var edge = getEdgeFor.GetFromHopEdge(way.Id.Value, minStartNode, endNode);
            if (edge.HasValue) fromEdges.Add(edge.Value);
        }

        if (fromEdges.Count == 0)
        {
            return new Result<IEnumerable<NetworkTurnRestriction>>(
                "could not parse any part of the from-part of the restriction");
        }

        // get to edges.
        var toEdges = new List<(EdgeId edge, bool forward)>();
        foreach (var hop in osmTurnRestriction.GetToHops())
        {
            if (hop.IsError) continue;
            var (way, startNode, maxEndNode) = hop.Value;

            // get the from-hop from the two nodes and the way id.
            var edge = getEdgeFor.GetToHopEdge(way.Id.Value, startNode, maxEndNode);
            if (edge.HasValue) toEdges.Add(edge.Value);
        }

        if (toEdges.Count == 0)
        {
            return new Result<IEnumerable<NetworkTurnRestriction>>(
                "could not parse any part of the to-part of the restriction");
        }

        // get the in between sequence.
        var viaEdges = new List<(EdgeId edgeId, bool forward)>();
        var viaSequences = osmTurnRestriction.GetViaHops();
        if (viaSequences.IsError) return viaSequences.ConvertError<IEnumerable<NetworkTurnRestriction>>();
        foreach (var hop in viaSequences.Value)
        {
            var (way, startNode, endNode) = hop;

            // get the from-hop from the two nodes and the way id.
            var edge = getEdgeFor.GetViaHopEdges(way.Id.Value, startNode, endNode);
            viaEdges.AddRange(edge);
        }

        var networkRestrictions = new List<NetworkTurnRestriction>();
        foreach (var fromEdge in fromEdges)
        foreach (var toEdge in toEdges)
        {
            var sequence = new List<(EdgeId edgeId, bool forward)> { fromEdge };
            sequence.AddRange(viaEdges);
            sequence.Add(toEdge);

            networkRestrictions.Add(new NetworkTurnRestriction(sequence, osmTurnRestriction.IsProbibitory,
                osmTurnRestriction.Attributes));
        }

        return networkRestrictions;
    }

    /// <summary>
    /// Gets the node that connects the from ways to the via way(s) or nodes.
    /// </summary>
    /// <param name="osmTurnRestriction">The turn restriction.</param>
    /// <returns>The node where the from ways end.</returns>
    public static Result<long> GetViaFrom(this OsmTurnRestriction osmTurnRestriction)
    {
        // assume the restriction has a via-node like most of them.
        var node = osmTurnRestriction.ViaNodeId;
        if (node != null) return node.Value;

        // but some have via-ways, so get the node from the first via-way.
        var viaWay = osmTurnRestriction.Via.FirstOrDefault();
        if (viaWay == null) return new Result<long>("no via node or via way found");
        foreach (var fromWay in osmTurnRestriction.From)
        {
            if (fromWay.Nodes[^1] == viaWay.Nodes[0] ||
                fromWay.Nodes[^1] == viaWay.Nodes[^1])
            {
                return fromWay.Nodes[^1];
            }

            if (fromWay.Nodes[0] == viaWay.Nodes[0] ||
                fromWay.Nodes[0] == viaWay.Nodes[^1])
            {
                return fromWay.Nodes[0];
            }
        }

        return new Result<long>("no via node found to end the from ways");
    }

    /// <summary>
    /// Enumerates from ways and their nodes in the direction of the restricted sequence.
    /// </summary>
    /// <param name="osmTurnRestriction">The turn restriction.</param>
    /// <returns>The from ways and for each the start node index where the sequence starts at the earliest and the end node index.</returns>
    public static IEnumerable<Result<(Way way, int minStartNode, int endNode)>> GetFromHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var node = osmTurnRestriction.GetViaFrom();
        
        var fromWayRestriction = new List<Result<(Way way, int minStartNode, int endNode)>>();
        foreach (var fromWay in osmTurnRestriction.From)
        {
            if (node.IsError)
            {
                fromWayRestriction.Add(node.ConvertError<(Way way, int minStartNode, int endNode)>());
                continue;
            }

            if (fromWay.Nodes[^1] == node)
            {
                fromWayRestriction.Add((fromWay, 0, fromWay.Nodes.Length - 1));
                continue;
            }

            if (fromWay.Nodes[0] == node)
            {
                fromWayRestriction.Add((fromWay, fromWay.Nodes.Length - 1, 0));
            }
        }

        return fromWayRestriction;
    }

    /// <summary>
    /// Gets the node that connects the to ways to the via way(s) or nodes.
    /// </summary>
    /// <param name="osmTurnRestriction">The turn restriction.</param>
    /// <returns>The node where the to ways begin.</returns>
    public static Result<long> GetViaTo(this OsmTurnRestriction osmTurnRestriction)
    {
        // assume the restriction has a via-node like most of them.
        var node = osmTurnRestriction.ViaNodeId;
        if (node != null) return node.Value;

        // but some have via-ways, so get the node from the first via-way.
        var viaWay = osmTurnRestriction.Via.FirstOrDefault();
        if (viaWay == null) return new Result<long>("no via node or via way found");
        foreach (var toWay in osmTurnRestriction.To)
        {
            if (toWay.Nodes[^1] == viaWay.Nodes[0] ||
                toWay.Nodes[^1] == viaWay.Nodes[^1])
            {
                return toWay.Nodes[^1];
            }

            if (toWay.Nodes[0] == viaWay.Nodes[0] ||
                toWay.Nodes[0] == viaWay.Nodes[^1])
            {
                return toWay.Nodes[0];
            }
        }

        return new Result<long>("no via node found to start the to ways");
    }

    /// <summary>
    /// Enumerates to ways and their nodes in the direction of the restricted sequence.
    /// </summary>
    /// <param name="osmTurnRestriction">The turn restriction.</param>
    /// <returns>The to ways and for each the node where the to sequence starts and where it ends at the latest.</returns>
    public static IEnumerable<Result<(Way way, int startNode, int maxEndNode)>> GetToHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var node = osmTurnRestriction.GetViaTo();

        // assume the restriction has a via-node like most of them.
        var toWayResults = new List<Result<(Way way, int startNode, int maxEndNode)>>();
        foreach (var toWay in osmTurnRestriction.To)
        {
            if (node.IsError)
            {
                toWayResults.Add(node.ConvertError<(Way way, int startNode, int endNode)>());
                continue;
            }

            if (toWay.Nodes[0] == node)
            {
                toWayResults.Add((toWay, 0, toWay.Nodes.Length - 1));
                continue;
            }

            if (toWay.Nodes[^1] == node)
            {
                toWayResults.Add((toWay, toWay.Nodes.Length - 1, 0));
            }
        }

        return toWayResults;
    }

    /// <summary>
    /// Gets the via way segments.
    /// </summary>
    /// <param name="osmTurnRestriction">The OSM turn restrictions.</param>
    /// <returns>The ways and the two node indexes representing the via part.</returns>
    public static Result<IReadOnlyList<(Way via, int startNode, int endNode)>> GetViaHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var node1 = osmTurnRestriction.GetViaFrom();
        if (node1.IsError) return node1.ConvertError<IReadOnlyList<(Way via, int node1Idx, int node2Idx)>>();
        var node2 = osmTurnRestriction.GetViaTo();
        if (node2.IsError) return node2.ConvertError<IReadOnlyList<(Way via, int node1Idx, int node2Idx)>>();

        if (node1.Value == node2.Value)
        {
            // the most default case, one via node.
            return Array.Empty<(Way via, int node1Idx, int node2Idx)>();
        }

        // there have to be via ways at this point.
        // it is assumed ways are split to follow along the sequence.
        var currentNode = node1.Value;
        var sequences = new List<(Way via, int node1Idx, int node2Idx)>();
        foreach (var viaWay in osmTurnRestriction.Via)
        {
            if (viaWay.Nodes[0] == currentNode)
            {
                sequences.Add((viaWay, 0, viaWay.Nodes.Length - 1));
                currentNode = viaWay.Nodes[^1];
            }
            else if (viaWay.Nodes[^1] == currentNode)
            {
                sequences.Add((viaWay, viaWay.Nodes.Length - 1, 0));
                currentNode = viaWay.Nodes[0];
            }
            else
            {
                return new Result<IReadOnlyList<(Way via, int startNode, int endNode)>>(
                    "one of via ways does not fit in a sequence");
            }
        }

        if (currentNode != node2)
            return new Result<IReadOnlyList<(Way via, int startNode, int endNode)>>(
                "last node of via sequence does not match");

        return sequences;
    }

    private static (EdgeId edge, bool forward)? GetFromHopEdge(this GetEdgeFor getEdgeFor, long wayId, int minStartNode, int endNode)
    {
        if (minStartNode < endNode)
        {
            for (var n = endNode - 1; n >= minStartNode; n--)
            {
                var edge = getEdgeFor(wayId, n, endNode);
                if (edge != null) return edge;
            }
        }
        else
        {
            for (var n = endNode + 1; n <= minStartNode; n++)
            {
                var edge = getEdgeFor(wayId, n, endNode);
                if (edge != null) return edge;
            }
        }

        return null;
    }

    private static IEnumerable<(EdgeId edge, bool forward)> GetViaHopEdges(this GetEdgeFor getEdgeFor, long wayId,
        int startNode, int endNode)
    {
        if (startNode < endNode)
        {
            for (var n = startNode + 1; n <= endNode; n++)
            {
                var edge = getEdgeFor(wayId, startNode, n);
                if (edge == null) continue;
                
                startNode = n;
                yield return edge.Value;
            }
        }
        else
        {
            for (var n = startNode - 1; n >= endNode; n--)
            {
                var edge = getEdgeFor(wayId, startNode, n);
                if (edge == null) continue;
                
                startNode = n;
                yield return edge.Value;
            }
        }
    }

    private static (EdgeId edge, bool forward)? GetToHopEdge(this GetEdgeFor getEdgeFor, long wayId, int startNode, int maxEndNode)
    {
        if (startNode < maxEndNode)
        {
            for (var n = startNode + 1; n <= maxEndNode; n++)
            {
                var edge = getEdgeFor(wayId, startNode, n);
                if (edge != null) return edge;
            }
        }
        else
        {
            for (var n = startNode - 1; n >= maxEndNode; n--)
            {
                var edge = getEdgeFor(wayId, startNode, n);
                if (edge != null) return edge;
            }
        }

        return null;
    }
}