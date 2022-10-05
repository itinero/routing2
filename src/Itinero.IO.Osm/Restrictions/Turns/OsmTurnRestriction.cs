using System;
using System.Collections.Generic;
using OsmSharp;

namespace Itinero.IO.Osm.Restrictions;

/// <summary>
/// The original OSM turn restriction.
/// </summary>
public class OsmTurnRestriction
{
    /// <summary>
    /// Creates a restriction with a via-node.
    /// </summary>
    /// <param name="froms">One or more from ways.</param>
    /// <param name="viaNodeId">The via node id.</param>
    /// <param name="tos">One or more to ways.</param>
    /// <param name="isProhibitory">The prohibitory flag.</param>
    /// <param name="attributes">The attributes.</param>    
    /// <returns>An OSM turn restriction.</returns>
    public static OsmTurnRestriction Create(IEnumerable<Way> froms, long viaNodeId, IEnumerable<Way> tos, bool isProhibitory = true,
        IEnumerable<(string key, string value)>? attributes = null)
    {
        attributes ??= ArraySegment<(string key, string value)>.Empty;

        return new OsmTurnRestriction()
        {
            IsProbibitory = isProhibitory,
            From = froms,
            ViaNodeId = viaNodeId,
            Via = ArraySegment<Way>.Empty,
            To = tos,
            Attributes = attributes
        };
    }

    /// <summary>
    /// Creates a restriction with a via-node.
    /// </summary>
    /// <param name="froms">One or more from ways.</param>
    /// <param name="vias">One or more via ways.</param>
    /// <param name="tos">One or more to ways.</param>
    /// <param name="isProhibitory">The prohibitory flag.</param>
    /// <param name="attributes">The attributes.</param>
    /// <returns>An OSM turn restriction.</returns>
    public static OsmTurnRestriction Create(IEnumerable<Way> froms, IEnumerable<Way> vias, IEnumerable<Way> tos, bool isProhibitory = true,
        IEnumerable<(string key, string value)>? attributes = null)
    {
        attributes ??= ArraySegment<(string key, string value)>.Empty;

        return new OsmTurnRestriction()
        {
            IsProbibitory = isProhibitory,
            From = froms,
            ViaNodeId = null,
            Via = vias,
            To = tos,
            Attributes = attributes
        };
    }

    /// <summary>
    /// A flag set to true when this restriction is prohibitory, false when mandatory.
    /// </summary>
    public bool IsProbibitory { get; private set; }

    /// <summary>
    /// The attributes associated with the turn.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes { get; private set; }

    /// <summary>
    /// The from way(s).
    /// </summary>
    public IEnumerable<Way> From { get; private set; }

    /// <summary>
    /// The via way(s), if any.
    /// </summary>
    public IEnumerable<Way> Via { get; private set; }

    /// <summary>
    /// The via node id, if any.
    /// </summary>
    public long? ViaNodeId { get; private set; }

    /// <summary>
    /// The to way(s).
    /// </summary>
    public IEnumerable<Way> To { get; private set; }
}
