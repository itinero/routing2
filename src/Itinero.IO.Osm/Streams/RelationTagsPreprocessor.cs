using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams.Filters;

namespace Itinero.IO.Osm.Streams
{
    /// <summary>
    /// A relation tags preprocessor.
    /// </summary>
    /// <remarks>
    /// Allows to add tags to relation members based on their membership.
    /// </remarks>
    internal class RelationTagsPreprocessor : OsmStreamFilter
    {
        private readonly Func<Relation, OsmGeo?, bool> _action;
        private readonly Dictionary<OsmGeoKey, RelationLinked> _parentRelations;

        private OsmGeo? _current;

        public RelationTagsPreprocessor(Func<Relation, OsmGeo?, bool> action)
        {
            _action = action;

            _parentRelations = new Dictionary<OsmGeoKey, RelationLinked>();
        }

        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            _current = null;
            if (!this.Source.MoveNext(ignoreNodes, ignoreWays, ignoreRelations)) return false;

            _current = this.Source.Current();

            // register parent for all member if relevant relation.
            switch (_current)
            {
                case Relation r:
                    // if not relevant, take no more actions.
                    if (_action(r, null))
                    {
                        foreach (var m in r.Members)
                        {
                            var key = new OsmGeoKey(m.Type, m.Id);
                            if (!_parentRelations.TryGetValue(key, out var e))
                            {
                                e = null;
                            }

                            _parentRelations[key] = new RelationLinked()
                            {
                                Relation = r,
                                Next = e
                            };
                        }
                    }
                    break;
            }

            // check if this is a child.
            var currentKey = new OsmGeoKey(_current);
            if (!_parentRelations.TryGetValue(currentKey, out var parent))
            {
                return true;
            }
            while (parent != null)
            {
                _action(parent.Relation, _current);
                parent = parent.Next;
            }

            return true;
        }

        public override OsmGeo Current()
        {
            if (_current == null)
                throw new InvalidOperationException(
                    $"Current is null, do {nameof(MoveNext)} first.");

            return _current;
        }

        public override void Reset()
        {
            this.Source.Reset();
        }

        public override bool CanReset => this.Source.CanReset;

        private class RelationLinked
        {
            public Relation Relation { get; set; } = null!;

            public RelationLinked? Next { get; set; }
        }
    }
}
