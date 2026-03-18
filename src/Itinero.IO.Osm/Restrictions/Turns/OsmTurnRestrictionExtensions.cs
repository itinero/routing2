using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.IO.Osm.Restrictions.Turns;

public static class OsmTurnRestrictionExtensions
{
    /// <summary>
    /// Converts the given OSM turn restriction into one or more sequences on the network.
    /// </summary>
    /// <param name="osmTurnRestriction">The OSM turn restriction.</param>
    /// <returns>The restriction using network edges and vertices.</returns>
    public static IEnumerable<GlobalRestriction> ToGlobalNetworkRestrictions(this OsmTurnRestriction osmTurnRestriction)
    {
        var viaSequences = osmTurnRestriction.GetViaHops();
        if (viaSequences == null) yield break;

        foreach (var tailEdge in osmTurnRestriction.GetTailHops())
            foreach (var headEdge in osmTurnRestriction.GetHeadHops())
            {
                IEnumerable<GlobalEdgeId> edges = [tailEdge];
                edges = edges.Concat(viaSequences).Concat([headEdge]);

                yield return new GlobalRestriction(edges,
                    osmTurnRestriction.IsProbibitory, osmTurnRestriction.Attributes);
            }
    }

    private static long? GetViaTail(this OsmTurnRestriction osmTurnRestriction)
    {
        // assume the restriction has a via-node like most of them.
        var node = osmTurnRestriction.ViaNodeId;
        if (node != null) return node.Value;

        // but some have via-ways, so get the node from the first via-way.
        var viaWay = osmTurnRestriction.Via.FirstOrDefault();
        if (viaWay == null) return null;
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

        // not cool, probably restriction not mapped correctly.
        return null;
    }

    private static IEnumerable<GlobalEdgeId> GetTailHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var node = osmTurnRestriction.GetViaTail();
        if (node == null) yield break;

        foreach (var fromWay in osmTurnRestriction.From)
        {
            if (fromWay.Nodes[^1] == node)
            {
                yield return GlobalEdgeId.Create(fromWay.Id!.Value, 0, fromWay.Nodes.Length - 1);
                continue;
            }

            if (fromWay.Nodes[0] == node)
            {
                yield return GlobalEdgeId.Create(fromWay.Id!.Value, fromWay.Nodes.Length - 1, 0);
            }
        }
    }

    private static long? GetViaHead(this OsmTurnRestriction osmTurnRestriction)
    {
        // assume the restriction has a via-node like most of them.
        var node = osmTurnRestriction.ViaNodeId;
        if (node != null) return node.Value;

        // but some have via-ways, so get the node from the first via-way.
        var viaWay = osmTurnRestriction.Via.FirstOrDefault();
        if (viaWay == null) return null;
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

        return null;
    }

    private static IEnumerable<GlobalEdgeId> GetHeadHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var node = osmTurnRestriction.GetViaHead();
        if (node == null) yield break;

        // assume the restriction has a via-node like most of them.
        foreach (var toWay in osmTurnRestriction.To)
        {
            if (toWay.Nodes[0] == node)
            {
                yield return GlobalEdgeId.Create(toWay.Id!.Value, 0, toWay.Nodes.Length - 1);
                continue;
            }

            if (toWay.Nodes[^1] == node)
            {
                yield return GlobalEdgeId.Create(toWay.Id!.Value, toWay.Nodes.Length - 1, 0);
            }
        }
    }

    private static IReadOnlyList<GlobalEdgeId>? GetViaHops(
        this OsmTurnRestriction osmTurnRestriction)
    {
        var tailNode = osmTurnRestriction.GetViaTail();
        if (tailNode == null) return null;
        var headNode = osmTurnRestriction.GetViaHead();
        if (headNode == null) return null;

        // via is a node if true.
        if (tailNode.Value == headNode.Value) return ArraySegment<GlobalEdgeId>.Empty;

        // there have to be via ways at this point.
        // it is assumed ways are split to follow along the sequence.
        var currentNode = tailNode.Value;
        var edges = new List<GlobalEdgeId>();
        foreach (var viaWay in osmTurnRestriction.Via)
        {
            if (viaWay.Nodes[0] == currentNode)
            {
                edges.Add(GlobalEdgeId.Create(viaWay.Id!.Value, 0, viaWay.Nodes.Length - 1));
                currentNode = viaWay.Nodes[^1];
            }
            else if (viaWay.Nodes[^1] == currentNode)
            {
                edges.Add(GlobalEdgeId.Create(viaWay.Id!.Value, viaWay.Nodes.Length - 1, 0));
                currentNode = viaWay.Nodes[0];
            }
            else
            {
                return null;
            }
        }

        if (currentNode != headNode)
            return null;

        return edges;
    }
}
