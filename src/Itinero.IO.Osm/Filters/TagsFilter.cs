using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Complete;

namespace Itinero.IO.Osm.Filters;

/// <summary>
/// A tags filter.
/// </summary>
/// <remarks>
/// The filter processes objects as follows:
/// - First pass:
///   - The FilterAsComplete predicate is used, if any, to determine what objects should be built up as complete objects.
///   - The FilterChildren function is used, if any, to store the children where, in the second pass, another function needs to be ran.
/// - Second pass:
///   - The FilterChildren functions for the children is executed first, if any.
///   - The CompleteFilter is executed second, only on the objects that pass the FilterAsComplete predicate.
///   - The Filter is executed last, limiting the objects considered.
/// </remarks>
public sealed class TagsFilter
{
    /// <summary>
    /// Filters OSM tags and objects.
    /// </summary>
    /// <remarks>
    /// When false is returned, the object is not included.
    /// The delegate is also allowed to rewrite tags of the OsmGeo-object
    /// </remarks>
    public FilterDelegate? Filter { get; set; }
        
    /// <summary>
    /// A function signature to define a filter.
    /// </summary>
    /// <param name="osmGeo">The OSM object.</param>
    public delegate bool FilterDelegate(OsmGeo osmGeo);
        
    /// <summary>
    /// Filters OSM tags using a complete version of OSM objects.
    /// </summary>
    /// <remarks>
    /// When false is returned, no filter is applied to the given object.
    /// This function is called with the complete parameter empty in a first pass.
    /// </remarks>
    public CompleteFilterDelegate? CompleteFilter { get; set; }

    /// <summary>
    /// A function signature to define a complete filter.
    /// </summary>
    /// <param name="osmGeo">The OSM object.</param>
    /// <param name="completeOsmGeo">The complete OSM object, if available.</param>
    public delegate bool CompleteFilterDelegate(OsmGeo osmGeo, CompleteOsmGeo? completeOsmGeo);

    /// <summary>
    /// Filters OSM tags on children of OSM objects by applying another specific filter if an object is a child.
    /// </summary>
    /// <remarks>
    /// When false is returned, no child-specific filter is applied to the given relation. This function is called with the member parameter empty in a first pass.
    /// </remarks>
    public FilterRelationMemberDelegate? MemberFilter { get; set; }

    /// <summary>
    /// A function signature to define a member filter.
    /// </summary>
    /// <param name="relation">The relation.</param>
    /// <param name="member">The member.</param>
    public delegate bool FilterRelationMemberDelegate(Relation relation, OsmGeo? member);
}