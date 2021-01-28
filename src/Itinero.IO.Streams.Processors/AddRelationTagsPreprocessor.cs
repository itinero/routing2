using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Streams.Processors
{
    /// <summary>
    ///     Adds a predefined tag to ways and nodes if it is a member of a relation as specified
    /// </summary>
    public class AddRelationTagsPreprocessor : PreprocessorStream
    {
        private readonly Predicate<Relation> _doesMatch;
        private readonly string _key;

        /// <summary>
        ///     The ids of the nodes that should be tagged in the next round; and the value that should be applied on the according this._tag
        /// </summary>
        private readonly Dictionary<long, string> _nodeIds = new();

        /// <summary>
        ///     Mark the relation ids of which the member-ids have already been added to _nodeIds, _wayIds and _relationIds
        /// </summary>
        private readonly HashSet<long> _processedRelations = new();

        /// <summary>
        ///     The ids of the relations that should be tagged in the next round; and the value that should be applied on the according this._tag
        /// </summary>
        private readonly Dictionary<long, string> _relationIds = new();


        private readonly Func<Relation, string, string> _value;

        /// <summary>
        ///     The ids of the ways that should be tagged in the next round; and the value that should be applied on the according this._tag
        /// </summary>
        private readonly Dictionary<long, string> _wayIds = new();


        /// <summary>
        ///     A preprocessor which adds a tag onto every member of the specified relations
        /// </summary>
        /// <remarks>
        /// If the predicate matches multiple relations, all of the relations will be processed.
        /// If multiple such relations contain the same segment - and the processing will result in a different tag value to be applied,
        /// one of both tags will be applied to the object at random.
        /// </remarks>
        /// <param name="actualSource">The underlying source of all the geo objects</param>
        /// <param name="doesMatch">A predicate indicating that a key should be added</param>
        /// <param name="key">The key to add. Must start with '_relation:', otherwise an exception is thrown</param>
        /// <param name="value">A function that calculates the value based on the relation; <code>_ => "yes"</code> if none given</param>
        public AddRelationTagsPreprocessor(OsmStreamSource actualSource,
            Predicate<Relation> doesMatch,
            string key, Func<Relation, string, string> value = null
        ) : base(actualSource, 2)
        {
            _doesMatch = doesMatch;
            _key = key;
            _value = value ?? ((_, _) => "yes");
        }


        private void AddMembersToWishlist(Relation r)
        {
            if (r.Id != null) {
                if (_processedRelations.Contains(r.Id.Value)) {
                    return;
                }

                _processedRelations.Add(r.Id.Value);
            }

            foreach (var member in r.Members) {
                Dictionary<long, string> addTo = null;
                switch (member.Type) {
                    case OsmGeoType.Node:
                        addTo = _nodeIds;
                        break;
                    case OsmGeoType.Way:
                        addTo = _wayIds;
                        break;
                    case OsmGeoType.Relation:
                        addTo = _relationIds;
                        break;
                }

                if (addTo == null) {
                    // Huh? This is not a node, way or relation
                    throw new Exception("Got an OSMGEO object which isn't a node, way or relation");
                }

                addTo[member.Id] = _value.Invoke(r, member.Role);
            }
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private protected override void PreprocessFeature(OsmGeo feature)
        {
            if (feature is Relation r) {
                if (_doesMatch(r)) {
                    this.AddMembersToWishlist(r);
                }

                if (_relationIds.TryGetValue(r.Id.Value, out var tagValue)) {
                    r.Tags.Add(_key, tagValue);
                }
            }

            if (feature is Way w) {
                if (_wayIds.TryGetValue(w.Id.Value, out var tagValue)) {
                    w.Tags.Add(_key, tagValue);
                }
            }
            if (feature is Node n) {
                if (_wayIds.TryGetValue(n.Id.Value, out var tagValue)) {
                    n.Tags.Add(_key, tagValue);
                }
            }
        }
    }
}