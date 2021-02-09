using System;
using OsmSharp;
using OsmSharp.Complete;

namespace Itinero.IO.Osm.Streams
{
    /// <summary>
    /// A complete OSM preprocessor.
    /// </summary>
    /// <remarks>
    /// A preprocessor that allows update tags based using the complete version of OSM geo objects. This can be used for example to:
    /// - Add tags based on geometry of ways: for example length.
    /// - Add tags based on geometry of children of 
    /// </remarks>
    internal class CompleteOsmGeoPreprocessor
    {
        private readonly Predicate<OsmGeo> _isRelevant;

        public CompleteOsmGeoPreprocessor(Predicate<OsmGeo> _isRelevant, 
            Action<CompleteOsmGeo> action)
        {
            
        }
    }
}