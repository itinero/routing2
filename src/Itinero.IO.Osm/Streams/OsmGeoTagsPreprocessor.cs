using System;
using OsmSharp;
using OsmSharp.Streams.Filters;

namespace Itinero.IO.Osm.Streams
{
    /// <summary>
    /// An OSM tags preprocessor.
    /// </summary>
    internal class OsmGeoTagsPreprocessor : OsmStreamFilter
    {
        private readonly Func<OsmGeo, bool> _action;

        private OsmGeo? _current;

        public OsmGeoTagsPreprocessor(Func<OsmGeo, bool> action)
        {
            _action = action;
        }
        
        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            _current = null;
            
            while (true) {
                if (!this.Source.MoveNext(ignoreNodes, ignoreWays, ignoreRelations)) return false;
                
                var current = this.Source.Current();
                if (!_action(current)) continue;

                _current = current;
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
    }
}