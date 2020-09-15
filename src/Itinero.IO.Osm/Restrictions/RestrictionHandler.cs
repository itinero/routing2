using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Algorithms;
using Itinero.Data.Graphs;
using OsmSharp;
using OsmSharp.Db;

[assembly:InternalsVisibleTo("Itinero.Tests")]
namespace Itinero.IO.Osm.Restrictions
{
    /// <summary>
    /// Parses OSM restrictions. Input is an OSM relation restriction, output is one or more restricted sequences of vertices.
    /// </summary>
    /// <remarks>
    /// This was used a the main source: https://wiki.openstreetmap.org/wiki/Relation:restriction 
    /// </remarks>
    public static class RestrictionParser
    {
        private static readonly IReadOnlyDictionary<string, bool> RestrictionTypes = new Dictionary<string, bool>()
        {
            { "no_left_turn", true },
            { "no_right_turn", true },
            { "no_straight_on", true },
            { "no_u_turn", true },
            { "no_entry", true },
            { "only_right_turn", false },
            { "only_left_turn", false },
            { "only_straight_on", false }
        };

        /// <summary>
        /// Gets the sequence of edges for the given turn restriction relation.
        /// </summary>
        /// <param name="relation">The turn restriction relation.</param>
        /// <param name="getVertex">A function to get a vertex corresponding to a node.</param>
        /// <param name="getEdges">A function to get all edges for a given way.</param>
        /// <returns>A sequence of edges representing the turn restriction.</returns>
        public static Result<IEnumerable<(EdgeId edge, bool forward)>> GetEdgeSequence(this Relation relation,
            Func<long, VertexId?> getVertex,
            Func<long, IEnumerable<(VertexId from, VertexId to, EdgeId id)>> getEdges)
        {
            var membersResult = relation.ParseMemberRoles();
            if (membersResult.IsError) return membersResult.ConvertError<IEnumerable<(EdgeId edge, bool forward)>>();
            var members = membersResult.Value;
            
            // get via vertex.
            if (members.via.Type != OsmGeoType.Way) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("Restrictions with 'via' ways not supported.");
            var via = getVertex(members.via.Id);
            if (via == null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("Via node is not a vertex.");
            
            // get 'from' edges and match them with the via vertex.
            (EdgeId edge, bool forward)? from = null;
            foreach (var e in getEdges(members.fromWayId))
            {
                if (e.from == via)
                {
                    if (from != null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'from' edge could not be uniquely identified.");
                    from = (e.id, false);
                }
                else if (e.to == via)
                {
                    if (from != null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'from' edge could not be uniquely identified.");
                    from = (e.id, true);
                }
            }
            if (from == null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'from' edge not found.");
            
            // get 'to' edges and match them with the via vertex.
            (EdgeId edge, bool forward)? to = null;
            foreach (var e in getEdges(members.toWayId))
            {
                if (e.from == via)
                {
                    if (to != null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'to' edge could not be uniquely identified.");
                    to = (e.id, true);
                }
                else if (e.to == via)
                {
                    if (from != null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'to' edge could not be uniquely identified.");
                    to = (e.id, false);
                }
            }
            if (to == null) return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'to' edge not found.");
            
            return new Result<IEnumerable<(EdgeId edge, bool forward)>>(new [] { from.Value, to.Value });
        }

        private static Result<(long fromWayId, OsmGeoKey via, long toWayId)> ParseMemberRoles(this Relation relation)
        {
            if (relation.Members == null) return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Relation has no members.");
            
            long? fromWayId = null;
            OsmGeoKey? via = null;
            long? toWayId = null;
            foreach (var member in relation.Members)
            {
                switch (member.Role)
                {
                    case "from":
                        if (member.Type != OsmGeoType.Way) 
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Member in 'from' role is not a Way.");
                        fromWayId = member.Id;
                        break;
                    case "via":
                        if (member.Type == OsmGeoType.Relation) 
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Member in 'via' role is a Relation.");
                        via = new OsmGeoKey(member.Type, member.Id);
                        break;
                    case "to":
                        if (member.Type != OsmGeoType.Way) 
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Member in 'to' role is not a Way.");
                        toWayId = member.Id;
                        break;
                }
            }

            if (fromWayId == null) return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Relation has no member with 'from' role.");
            if (via == null) return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Relation has no member with 'via' role.");
            if (toWayId == null) return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Relation has no member with 'to' role.");

            return (fromWayId.Value, via.Value, toWayId.Value);
        }

        /// <summary>
        /// Returns true if the restriction is 'negative', false if 'positive'.
        /// </summary>
        /// <remarks> 
        /// 'negative' is for example a restriction that prohibits a single turn.
        /// 'positive' is a restriction that excludes all turn except the one represented by the restriction.
        /// </remarks>
        /// <param name="relation">The restriction relation.</param>
        /// <returns>True if the restriction is 'negative', false if 'positive'.</returns>
        public static Result<bool> IsNegative(this Relation relation)
        {            
            try
            {
                if (relation?.Tags == null) 
                    return new Result<bool>("Relation has no tags.");
                if (!relation.Tags.Contains("type", "restriction")) 
                    return new Result<bool>("Relation is not a restriction.");
                if (!relation.Tags.TryGetValue("restriction", out var restriction)) 
                    return new Result<bool>("Relation has no restriction tag.");
                if (!RestrictionTypes.TryGetValue(restriction, out var type)) 
                    return new Result<bool>($"Relation has unknown restriction type: {type}.");

                return type;
            }
            catch (Exception e)
            {
                return new Result<bool>($"Parsing failed with unhandled exception: {e}");
            }
        }

        // public static Result<IEnumerable<VertexId>> GetVertexSequence(this Relation relation,
        //     Func<long, VertexId?> getVertex,
        //     Func<OsmGeoKey, OsmGeo?> getOsmGeo)
        // {
        //     try
        //     {
        //         // ok the tags are fine at this point, check for from, via, to members.
        //         var membersResult = GetMembers(relation, getOsmGeo);
        //         if (membersResult.IsError) return membersResult.ConvertError<IEnumerable<VertexId>>();
        //         var members = membersResult.Value;
        //         
        //         // build vertex sequence.
        //         var fromPartResult = GetSequence(members.from, "from", members.vias, getVertex);
        //         if (fromPartResult.IsError) return fromPartResult.ConvertError<IEnumerable<VertexId>>();
        //         var fromPart = fromPartResult.Value;
        //         var toPartResult = GetSequence(members.to, "to", members.vias, getVertex);
        //         if (toPartResult.IsError) return toPartResult.ConvertError<IEnumerable<VertexId>>();
        //         var toPart = toPartResult.Value;
        //         var viaPartResult = GetViaSequence(members, fromPart.viaNode, toPart.viaNode, getVertex);
        //         var viaPart = viaPartResult.Value;
        //         var sequence = Concatenate(fromPart.vertex, viaPart, toPart.vertex);
        //
        //         return new Result<IEnumerable<VertexId>>(sequence);
        //     }
        //     catch (Exception e)
        //     {
        //         return new Result<IEnumerable<VertexId>>($"Parsing failed with unhandled exception: {e}");
        //     }
        // }

        // private static IEnumerable<VertexId> Concatenate(VertexId from, IEnumerable<VertexId> via, VertexId to)
        // {
        //     yield return from;
        //
        //     foreach (var v in via)
        //     {
        //         yield return v;
        //     }
        //
        //     yield return to;
        // }

        // private static Result<IEnumerable<VertexId>> GetViaSequence(
        //     (Way from, IEnumerable<OsmGeo> vias, Way to) members, long fromNode, long toNode,
        //     Func<long, VertexId?> getVertex)
        // {
        //     using var viaEnumerator = members.vias.GetEnumerator();
        //     if (!viaEnumerator.MoveNext()) return new Result<IEnumerable<VertexId>>("There are no 'via' members.");
        //     var viaCurrent = viaEnumerator.Current;
        //     if (viaCurrent is Node)
        //     {
        //         // via is a node.
        //
        //         // check if there is a second via, if so, restrictions is invalid.
        //         if (viaEnumerator.MoveNext())
        //             return new Result<IEnumerable<VertexId>>(
        //                 "There is a 'via' node but another way has been detected.");
        //
        //         // get the vertex from the via node.
        //         if (viaCurrent.Id == null)
        //             return new Result<IEnumerable<VertexId>>("There is a 'via' node but it has no valid id.");
        //         var viaVertex = getVertex(viaCurrent.Id.Value);
        //         if (viaVertex == null)
        //             return new Result<IEnumerable<VertexId>>("There is a 'via' node but it has no vertex.");
        //
        //         return new[] {viaVertex.Value};
        //     }
        //
        //     // via is one or more ways.
        //     if (!(viaCurrent is Way way)) return new Result<IEnumerable<VertexId>>("'via' member detected but it's not a way or a node.");
        //     var viaWayNetwork = new Dictionary<long, List<(Way way, bool forward)>>();
        //     viaWayNetwork.AddWay(way);
        //     while (viaEnumerator.MoveNext())
        //     {
        //         viaCurrent = viaEnumerator.Current;
        //         if (viaCurrent is Node) return new Result<IEnumerable<VertexId>>(
        //             "There is a 'via' node but another way has been detected.");
        //         if (!(viaCurrent is Way w)) return new Result<IEnumerable<VertexId>>("'via' member detected but it's not a way or a node.");
        //         viaWayNetwork.AddWay(w);
        //     }
        //     
        //     // start from start node.
        //     var path = new List<long>();
        //     var currentNode = fromNode;
        //     var settledWays = new HashSet<long>();
        //     while (currentNode != toNode)
        //     {
        //         var edges = viaWayNetwork.Get(currentNode, settledWays);
        //         if (edges.Count > 1) return new Result<IEnumerable<VertexId>>("'via' path is not uniquely determined.");
        //         if (edges.Count == 0) return new Result<IEnumerable<VertexId>>("'via' path not found.");
        //
        //         var edge = edges[0];
        //         settledWays.Add(edge.way.Id.Value);
        //         
        //         // revers if not forward.
        //         var nodes = new List<long>(edge.way.Nodes);
        //         if (!edge.forward) nodes.Reverse();
        //         
        //         // add nodes.
        //         if (path.Count > 0) nodes.RemoveAt(0);
        //         path.AddRange(nodes);
        //
        //         currentNode = path[path.Count - 1];
        //     }
        //     
        //     return new Result<IEnumerable<VertexId>>(path.Select(n =>
        //     {
        //         var v = getVertex(n);
        //         if (v == null) throw new Exception($"No vertex found for node {n}.");
        //         return v.Value;
        //     }));
        // }
        //
        // private static List<(Way way, bool forward)> Get(this IDictionary<long, List<(Way way, bool forward)>> network,
        //     long node, ISet<long> settled)
        // {
        //     if (!network.TryGetValue(node, out var result)) return new List<(Way way, bool forward)>(0);
        //
        //     result = new List<(Way way, bool forward)>(result);
        //     result.RemoveAll(w => settled.Contains(w.way.Id.Value));
        //     return result;
        // }
        //
        // private static void AddWay(this IDictionary<long, List<(Way way, bool forward)>> network, Way way)
        // {
        //     if (way?.Nodes == null) return;
        //     if (way.Nodes.Length < 2) return;
        //     
        //     var node = way.Nodes[0];
        //     if (!network.TryGetValue(node, out var edges))
        //     {
        //         edges = new List<(Way way, bool forward)>();
        //         network[node] = edges;
        //     }
        //     edges.Add((way, true));
        //     node = way.Nodes[way.Nodes.Length - 1];
        //     if (!network.TryGetValue(node, out edges))
        //     {
        //         edges = new List<(Way way, bool forward)>();
        //         network[node] = edges;
        //     }
        //     edges.Add((way, false));
        // }
        //
        // private static Result<(VertexId vertex, VertexId viaVertex, long viaNode)> GetSequence(Way from, string role, IEnumerable<OsmGeo> vias,
        //     Func<long, VertexId?> getVertex)
        // {
        //     if (from.Nodes == null || from.Nodes.Length == 0) return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"'{role}' way has no nodes.");
        //     if (from.Nodes.Length == 1) return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"'from' way has only one node.");
        //     
        //     var fromFirst = from.Nodes[0];
        //     var fromLast = from.Nodes[from.Nodes.Length - 1];
        //
        //     bool? isFirst(long id)
        //     {
        //         if (id == fromFirst) return true;
        //         if (id == fromLast) return false;
        //         return null;
        //     }
        //
        //     bool? first = null;
        //     foreach (var via in vias)
        //     {
        //         if (via is Node)
        //         {
        //             if (via.Id == null) return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"'via' node has no valid id.");
        //             first = isFirst(via.Id.Value);
        //             break;
        //         }
        //         
        //         if (via is Way viaWay)
        //         {
        //             var viaFirst = viaWay.Nodes[0];
        //             first = isFirst(viaFirst);
        //             if (first != null) break;
        //             var viaLast = viaWay.Nodes[viaWay.Nodes.Length - 1];
        //             first = isFirst(viaLast);
        //             if (first != null) break;
        //         }
        //     }
        //     
        //     if (first == null) return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"'via' node not found in '{role}' way.");
        //
        //     var nodes = new List<long>(from.Nodes);
        //     if (!first.Value)
        //     {
        //         nodes.Reverse();
        //     }
        //
        //     var firstVertex = getVertex(nodes[0]);
        //     if (firstVertex == null) return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"The vertex for the node joining 'via' and '{role}' not found.");
        //
        //     for (var n = 1; n < nodes.Count; n++)
        //     {
        //         var secondVertex = getVertex(nodes[n]);
        //         if (secondVertex == null) continue;
        //         
        //         return (secondVertex.Value, firstVertex.Value, nodes[0]);
        //     }
        //     return new Result<(VertexId vertex, VertexId viaVertex, long viaNode)>($"There is no second vertex found for the nodes in the '{role}' way.");
        // }
        //
        // private static Result<(Way from, IEnumerable<OsmGeo> vias, Way to)> GetMembers(Relation relation, Func<OsmGeoKey, OsmGeo?> getOsmGeo)
        // {
        //     if (relation.Members == null) return new Result<(Way @from, IEnumerable<OsmGeo> vias, Way to)>("Relation has no members.");
        //     
        //     Way? from = null;
        //     var vias = new List<OsmGeo>(1);
        //     Way? to = null;
        //     foreach (var member in relation.Members)
        //     {
        //         switch (member.Role)
        //         {
        //             case "from":
        //                 @from = getOsmGeo(new OsmGeoKey(member.Type, member.Id)) as Way;
        //                 break;
        //             case "via":
        //                 var via = getOsmGeo(new OsmGeoKey(member.Type, member.Id));
        //                 if (via != null) vias.Add(via);
        //                 break;
        //             case "to":
        //                 to = getOsmGeo(new OsmGeoKey(member.Type, member.Id)) as Way;
        //                 break;
        //         }
        //     }
        //
        //     if (from == null) return new Result<(Way @from, IEnumerable<OsmGeo> vias, Way to)>("Relation has no member with 'from' role.");
        //     if (vias.Count == 0) return new Result<(Way @from, IEnumerable<OsmGeo> vias, Way to)>("Relation has no member with 'via' role.");
        //     if (to == null) return new Result<(Way @from, IEnumerable<OsmGeo> vias, Way to)>("Relation has no member with 'to' role.");
        //
        //     return (from, vias, to);
        // }
    }
}