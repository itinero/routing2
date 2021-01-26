using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Network;
using OsmSharp;
using OsmSharp.Db;

[assembly: InternalsVisibleTo("Itinero.Tests")]

namespace Itinero.IO.Osm.Restrictions {
    /// <summary>
    /// Parses OSM restrictions. Input is an OSM relation restriction, output is one or more restricted sequences of vertices.
    /// </summary>
    /// <remarks>
    /// This was used a the main source: https://wiki.openstreetmap.org/wiki/Relation:restriction 
    /// </remarks>
    public static class RestrictionParser {
        private static readonly IReadOnlyDictionary<string, bool> RestrictionTypes = new Dictionary<string, bool> {
            {"no_left_turn", true},
            {"no_right_turn", true},
            {"no_straight_on", true},
            {"no_u_turn", true},
            {"no_entry", true},
            {"only_right_turn", false},
            {"only_left_turn", false},
            {"only_straight_on", false}
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
            Func<long, IEnumerable<(VertexId from, VertexId to, EdgeId id)>> getEdges) {
            var membersResult = relation.ParseMemberRoles();
            if (membersResult.IsError) {
                return membersResult.ConvertError<IEnumerable<(EdgeId edge, bool forward)>>();
            }

            var members = membersResult.Value;

            // get via vertex.
            if (members.via.Type == OsmGeoType.Way) {
                return new Result<IEnumerable<(EdgeId edge, bool forward)>>(
                    "Restrictions with 'via' ways not supported.");
            }

            var via = getVertex(members.via.Id);
            if (via == null) {
                return new Result<IEnumerable<(EdgeId edge, bool forward)>>("Via node is not a vertex.");
            }

            // get 'from' edges and match them with the via vertex.
            (EdgeId edge, bool forward)? from = null;
            foreach (var e in getEdges(members.fromWayId)) {
                if (e.from == via) {
                    if (from != null) {
                        return new Result<IEnumerable<(EdgeId edge, bool forward)>>(
                            "'from' edge could not be uniquely identified.");
                    }

                    from = (e.id, false);
                }
                else if (e.to == via) {
                    if (from != null) {
                        return new Result<IEnumerable<(EdgeId edge, bool forward)>>(
                            "'from' edge could not be uniquely identified.");
                    }

                    from = (e.id, true);
                }
            }

            if (from == null) {
                return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'from' edge not found.");
            }

            // get 'to' edges and match them with the via vertex.
            (EdgeId edge, bool forward)? to = null;
            foreach (var e in getEdges(members.toWayId)) {
                if (e.from == via) {
                    if (to != null) {
                        return new Result<IEnumerable<(EdgeId edge, bool forward)>>(
                            "'to' edge could not be uniquely identified.");
                    }

                    to = (e.id, true);
                }
                else if (e.to == via) {
                    if (from != null) {
                        return new Result<IEnumerable<(EdgeId edge, bool forward)>>(
                            "'to' edge could not be uniquely identified.");
                    }

                    to = (e.id, false);
                }
            }

            if (to == null) {
                return new Result<IEnumerable<(EdgeId edge, bool forward)>>("'to' edge not found.");
            }

            return new Result<IEnumerable<(EdgeId edge, bool forward)>>(new[] {from.Value, to.Value});
        }

        private static Result<(long fromWayId, OsmGeoKey via, long toWayId)> ParseMemberRoles(this Relation relation) {
            if (relation.Members == null) {
                return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>("Relation has no members.");
            }

            long? fromWayId = null;
            OsmGeoKey? via = null;
            long? toWayId = null;
            foreach (var member in relation.Members) {
                switch (member.Role) {
                    case "from":
                        if (member.Type != OsmGeoType.Way) {
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                                "Member in 'from' role is not a Way.");
                        }

                        fromWayId = member.Id;
                        break;
                    case "via":
                        if (member.Type == OsmGeoType.Relation) {
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                                "Member in 'via' role is a Relation.");
                        }

                        via = new OsmGeoKey(member.Type, member.Id);
                        break;
                    case "to":
                        if (member.Type != OsmGeoType.Way) {
                            return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                                "Member in 'to' role is not a Way.");
                        }

                        toWayId = member.Id;
                        break;
                }
            }

            if (fromWayId == null) {
                return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                    "Relation has no member with 'from' role.");
            }

            if (via == null) {
                return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                    "Relation has no member with 'via' role.");
            }

            if (toWayId == null) {
                return new Result<(long fromWayId, OsmGeoKey via, long toWayId)>(
                    "Relation has no member with 'to' role.");
            }

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
        public static Result<bool> IsNegative(this Relation relation) {
            try {
                if (relation?.Tags == null) {
                    return new Result<bool>("Relation has no tags.");
                }

                if (!relation.Tags.Contains("type", "restriction")) {
                    return new Result<bool>("Relation is not a restriction.");
                }

                if (!relation.Tags.TryGetValue("restriction", out var restriction)) {
                    return new Result<bool>("Relation has no restriction tag.");
                }

                if (!RestrictionTypes.TryGetValue(restriction, out var type)) {
                    return new Result<bool>($"Relation has unknown restriction type: {type}.");
                }

                return type;
            }
            catch (Exception e) {
                return new Result<bool>($"Parsing failed with unhandled exception: {e}");
            }
        }
    }
}