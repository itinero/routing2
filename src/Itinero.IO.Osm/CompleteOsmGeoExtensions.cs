using System;
using OsmSharp;
using OsmSharp.Complete;

namespace Itinero.IO.Osm;

internal static class CompleteOsmGeoExtensions
{
    public static OsmGeo ToSimple(this ICompleteOsmGeo iCompleteOsmGeo)
    {
        if (!(iCompleteOsmGeo is CompleteOsmGeo completeOsmGeo))
        {
            throw new ArgumentOutOfRangeException(nameof(iCompleteOsmGeo));
        }

        return completeOsmGeo.ToSimple();
    }
}
