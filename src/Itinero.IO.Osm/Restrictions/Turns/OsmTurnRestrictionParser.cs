using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Db;

namespace Itinero.IO.Osm.Restrictions;

/// <summary>
/// Parses OSM restrictions. Input is an OSM relation restriction, output is one or more restricted sequences of vertices.
/// </summary>
/// <remarks>
/// This was used a the main source: https://wiki.openstreetmap.org/wiki/Relation:restriction 
/// </remarks>
public class OsmTurnRestrictionParser
{
    /// <summary>
    /// The default restriction types and their prohibitiveness.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> DefaultRestrictionTypes = new Dictionary<string, bool>
    {
        { "no_left_turn", true },
        { "no_right_turn", true },
        { "no_straight_on", true },
        { "no_u_turn", true },
        { "no_entry", true },
        { "no_exit", true },
        { "only_right_turn", false },
        { "only_left_turn", false },
        { "only_straight_on", false },
        { "only_u_turn", false }
    };

    /// <summary>
    /// The default vehicle types list.
    /// </summary>
    public static readonly ISet<string> DefaultVehicleTypes = new HashSet<string>()
    {
        "motor_vehicle",
        "foot",
        "dog",
        "bicycle",
        "motorcar",
        "hgv",
        "psv",
        "emergency"
    };

    /// <summary>
    /// The default vehicle type if none is specified.
    /// </summary>
    public static string DefaultVehicleType = "motor_vehicle";

    private readonly ISet<string> _vehicleTypes;
    private readonly IReadOnlyDictionary<string, bool> _restrictionTypes;
    private readonly string _defaultVehicleType;

    /// <summary>
    /// Creates a new restriction parser.
    /// </summary>
    /// <param name="vehicleTypes">The vehicle types.</param>
    /// <param name="restrictionTypes">The restriction types.</param>
    /// <param name="defaultVehicleType">The default vehicle type.</param>
    public OsmTurnRestrictionParser(ISet<string>? vehicleTypes = null,
        IReadOnlyDictionary<string, bool>? restrictionTypes = null,
        string? defaultVehicleType = null)
    {
        _defaultVehicleType = defaultVehicleType ?? DefaultVehicleType;
        _vehicleTypes = vehicleTypes ?? DefaultVehicleTypes;
        _restrictionTypes = restrictionTypes ?? DefaultRestrictionTypes;
    }

    /// <summary>
    /// Trims the attributes collection keeping only attributes that are relevant to represent a restriction.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>The relevant attributes.</returns>
    public IEnumerable<(string key, string value)> Trim(IEnumerable<(string key, string value)> attributes)
    {
        foreach (var (key, value) in attributes)
        {
            if (key.StartsWith("restriction:"))
            {
                var vehicleType = key[12..];
                if (!_vehicleTypes.Contains(vehicleType)) continue;
                if (!_restrictionTypes.ContainsKey(value)) continue;
            }
            else
            {
                switch (key)
                {
                    case "type":
                        if (value != "restriction") continue;
                        break;
                    case "restriction":
                        if (!_restrictionTypes.ContainsKey(value)) continue;
                        break;
                    case "except":
                        var exceptions = value.Split(';').ToList();
                        exceptions.RemoveAll(v => !_vehicleTypes.Contains(v));
                        if (exceptions.Count == 0) continue;
                        break;
                }
            }

            yield return (key, value);
        }
    }

    /// <summary>
    /// Returns true if by the tags only, the relation is a turn restriction.
    /// </summary>
    /// <param name="relation">The relation.</param>
    /// <param name="isProhibitive">True if the restriction restricts, false if it allows the turn it represents.</param>
    /// <returns>True if the relation is a restriction.</returns>
    public bool IsRestriction(Relation relation, out bool isProhibitive)
    {
        isProhibitive = false;
        var vehicleType = string.Empty;
        var exceptions = new List<string>();

        if (relation?.Tags == null)
        {
            return false;
        }

        if (!relation.Tags.Contains("type", "restriction"))
        {
            return false;
        }

        if (relation.Tags.TryGetValue("except", out var exceptValue))
        {
            exceptions.AddRange(exceptValue.Split(';'));
        }

        if (!relation.Tags.TryGetValue("restriction", out var restrictionValue))
        {
            foreach (var tag in relation.Tags)
            {
                if (!tag.Key.StartsWith("restriction:")) continue;

                vehicleType = tag.Key[12..];
                restrictionValue = tag.Value;
                break;
            }
        }
        else
        {
            vehicleType = _defaultVehicleType;
        }

        if (restrictionValue == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            return false;
        }

        if (!_vehicleTypes.Contains(vehicleType))
        {
            return false;
        }

        exceptions.RemoveAll(v => !_vehicleTypes.Contains(v));

        if (!_restrictionTypes.TryGetValue(restrictionValue, out isProhibitive))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to parse an OSM Relation into an OSM turn restriction.
    /// </summary>
    /// <param name="relation">The restriction relation.</param>
    /// <param name="getMemberWay">Function to get the member way.</param>
    /// <param name="restriction">The turn restriction.</param>
    /// <returns>Returns true if the relation is a restriction, false if not. Returns an error result if parsing failed for a relation that is tagged as a turn restriction.</returns>
    public Result<bool> TryParse(Relation relation, Func<long, Way?> getMemberWay,
        out OsmTurnRestriction? restriction)
    {
        restriction = null;
        if (!this.IsRestriction(relation, out var isProhibitive)) return false;

        var froms = new List<Way>();
        var vias = new List<Way>();
        var tos = new List<Way>();
        long? viaNodeId = null;
        foreach (var m in relation.Members)
        {
            if (m == null) return new Result<bool>("not all members set");
            if (m.Role != "via" && m.Role != "from" && m.Role != "to") continue;

            switch (m.Type)
            {
                case OsmGeoType.Node:
                    if (m.Role == "via")
                    {
                        viaNodeId = m.Id;
                    }
                    break;
                case OsmGeoType.Way:
                    if (m.Role is not "from" and not "to" and not "via") continue;
                    var wayMember = getMemberWay(m.Id);
                    if (wayMember == null) return new Result<bool>("member way not found");
                    switch (m.Role)
                    {
                        case "via":
                            vias.Add(wayMember);
                            break;
                        case "from":
                            froms.Add(wayMember);
                            break;
                        default:
                            tos.Add(wayMember);
                            break;
                    }
                    break;
                case OsmGeoType.Relation:
                    break;
                default:
                    break;
            }
        }

        // 2 options:
        // - with via-node.
        // - with via-way(s).

        var attributes = relation.Tags?.Select(tag => (tag.Key, tag.Value)).ToArray() ??
                         ArraySegment<(string key, string value)>.Empty;
        if (viaNodeId != null)
        {
            restriction = OsmTurnRestriction.Create(froms, viaNodeId.Value, tos, isProhibitive, attributes);
        }
        else if (vias.Count > 0)
        {
            restriction = OsmTurnRestriction.Create(froms, vias, tos, isProhibitive, attributes);
        }
        else
        {
            return new Result<bool>("no via nodes found");
        }

        return true;
    }
}
