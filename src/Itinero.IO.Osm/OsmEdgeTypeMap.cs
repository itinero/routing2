using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Itinero.Indexes;

namespace Itinero.IO.Osm;

/// <summary>
/// An edge type map that keeps only OSM tags relevant for routing.
/// Both key AND value are validated — only known routing-relevant key-value pairs are kept.
/// This ensures edges with different routing characteristics get different edgeTypeIds,
/// while irrelevant tags are stripped to maximize deduplication.
/// </summary>
public class OsmEdgeTypeMap : AttributeSetMap
{
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> RoutingTags;

    static OsmEdgeTypeMap()
    {
        var tags = new Dictionary<string, ImmutableHashSet<string>>();

        // access
        tags["access"] = Set("customers", "delivery", "designated", "destination", "dismount",
            "no", "permissive", "permit", "private", "service");
        tags["bicycle"] = Set("designated", "dismount", "no", "official", "permissive",
            "permit", "private", "use_sidepath", "yes");
        tags["foot"] = Set("designated", "no", "official", "permissive", "permit",
            "private", "use_sidepath", "yes");
        tags["hgv"] = Set("no", "permissive", "permit", "private", "yes");
        tags["motor_vehicle"] = Set("customers", "destination", "no", "permissive",
            "permit", "private", "yes");
        tags["motorcar"] = Set("customers", "destination", "no", "permissive",
            "permit", "private", "yes");
        tags["emergency"] = Set("yes");
        tags["motorroad"] = Set("yes");
        tags["area"] = Set("yes");

        // road classification — wildcard, accept any value
        tags["highway"] = ImmutableHashSet<string>.Empty;
        tags["route"] = ImmutableHashSet<string>.Empty;

        tags["service"] = Set("alley", "bus", "driveway", "parking_aisle");
        tags["tracktype"] = Set("grade1", "grade2", "grade3", "grade4", "grade5");
        tags["railway"] = Set("abandoned");

        // direction
        tags["oneway"] = Set("-1", "1", "no", "yes");
        tags["oneway:bicycle"] = Set("-1", "1", "no", "yes");
        tags["oneway:foot"] = Set("yes");
        tags["junction"] = Set("roundabout");

        // speed — wildcard, validated as integer
        tags["maxspeed"] = ImmutableHashSet<string>.Empty;

        // surface
        tags["surface"] = Set("asphalt", "cobblestone", "compacted", "concrete",
            "concrete:lanes", "concrete:plates", "dirt", "earth", "fine_gravel",
            "grass", "grass_paver", "gravel", "ground", "metal", "mud", "paved",
            "paving_stones", "pebblestone", "sand", "sett", "snow",
            "unhewn_cobblestone", "unpaved", "wood", "woodchips");
        tags["smoothness"] = Set("bad", "excellent", "good", "intermediate", "very_good");
        tags["sidewalk:surface"] = Set("asphalt", "concrete", "paving_stones", "sett");

        // cycling
        tags["cycleway"] = Set("lane", "opposite", "opposite_lane", "opposite_share_busway",
            "opposite_track", "right", "share_busway", "shared", "shared_lane",
            "track", "yes");
        tags["cycleway:left"] = Set("lane", "opposite", "opposite_lane", "opposite_track",
            "share_busway", "shared", "shared_lane", "track", "yes");
        tags["cycleway:right"] = Set("lane", "share_busway", "shared", "shared_lane",
            "track", "yes");
        tags["cycleway:left:oneway"] = Set("no");
        tags["cycleway:right:oneway"] = Set("no");
        tags["cyclestreet"] = Set("yes");
        tags["cycle_highway"] = ImmutableHashSet<string>.Empty;
        tags["bicycle:class"] = Set("-1", "-2", "-3", "0", "1", "2", "3");
        tags["ramp:bicycle"] = Set("yes");
        tags["towpath"] = Set("yes");
        tags["designation"] = Set("towpath");

        // barriers
        tags["barrier"] = Set("bollard", "sump_buster");

        // incline
        tags["incline"] = Set("-10%", "-20%", "-30%", "0", "0%", "10%", "20%", "30%",
            "down", "up");

        // other routing-relevant — wildcards
        tags["type"] = ImmutableHashSet<string>.Empty;
        tags["network:type"] = ImmutableHashSet<string>.Empty;
        tags["operator"] = ImmutableHashSet<string>.Empty;
        tags["state"] = ImmutableHashSet<string>.Empty;

        RoutingTags = tags.ToImmutableDictionary();
    }

    /// <summary>
    /// Creates a new OSM edge type map with the default set of routing-relevant tags.
    /// </summary>
    public OsmEdgeTypeMap()
        : base(new Guid("45d22274-cb26-490c-9685-aab0ef7d7e9c"))
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<(string key, string value)> Map(
        IEnumerable<(string key, string value)> attributes)
    {
        return attributes.Where(a => IsRelevant(a.key, a.value));
    }

    /// <summary>
    /// Returns true if the key-value pair is relevant for routing.
    /// </summary>
    private static bool IsRelevant(string key, string value)
    {
        if (!RoutingTags.TryGetValue(key, out var allowedValues)) return false;

        // if allowed values are specified, check membership.
        if (allowedValues.Count > 0) return allowedValues.Contains(value);

        // wildcard keys — custom validation per key.
        return key switch
        {
            "maxspeed" => int.TryParse(value, out _),
            _ => true
        };
    }

    private static ImmutableHashSet<string> Set(params string[] values)
    {
        return values.ToImmutableHashSet();
    }
}
