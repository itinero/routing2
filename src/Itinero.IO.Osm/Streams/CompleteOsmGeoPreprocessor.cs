using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Db;
using OsmSharp.Streams.Filters;

namespace Itinero.IO.Osm.Streams
{
    /// <summary>
    /// A complete OSM preprocessor.
    /// </summary>
    /// <remarks>
    /// A preprocessor that allows update tags based using the complete version of OSM geo objects. This can be used for example to:
    /// - Add tags based on geometry of ways: for example length.
    /// </remarks>
    internal class CompleteOsmGeoPreprocessor : OsmStreamFilter
    {
        private readonly Predicate<OsmGeo> _isRelevant;
        private readonly Action<CompleteOsmGeo, OsmGeo> _action;
        private readonly Children _children = new ();

        public CompleteOsmGeoPreprocessor(Predicate<OsmGeo> isRelevant, 
            Action<CompleteOsmGeo, OsmGeo> action)
        {
            _isRelevant = isRelevant;
            _action = action;
        }

        private OsmGeo? _current;

        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            _current = null;
            if (!this.Source.MoveNext(ignoreNodes, ignoreWays, ignoreRelations)) return false;

            _current = this.Source.Current();

            // could be a child of another relevant object.
            _children.AddAsChild(_current);

            // if not relevant, take no more actions.
            if (!_isRelevant(_current)) {
                return true;
            }

            // see if we can complete object, if so, feed to action.
            var completed = _current.CreateComplete(_children);
            if (completed is CompleteOsmGeo completeOsmGeo) {
                _action(completeOsmGeo, _current);
            }

            // register children to keep.
            switch (_current) {
                case Way w:
                    if (w.Nodes == null) return true;
                    foreach (var n in w.Nodes) {
                        _children.RegisterAsChild(new OsmGeoKey(OsmGeoType.Node, n));
                    }

                    break;
                case Relation r:
                    foreach (var m in r.Members) {
                        _children.RegisterAsChild(new OsmGeoKey(m.Type, m.Id));
                    }

                    break;
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

        private class Children : IOsmGeoSource
        {
            private readonly Dictionary<OsmGeoKey, OsmGeo?> _children = new ();

            public void RegisterAsChild(OsmGeoKey key)
            {
                if (_children.ContainsKey(key)) return;

                _children[key] = null;
            }
            
            public void AddAsChild(OsmGeo osmGeo)
            {
                var key = new OsmGeoKey(osmGeo);
                if (!_children.ContainsKey(key)) return;

                _children[key] = osmGeo;
            }
            
            public OsmGeo? Get(OsmGeoType type, long id)
            {
                if (!_children.TryGetValue(new OsmGeoKey(type, id), out var c)) return null;

                return c;
            }
        }
    }
}