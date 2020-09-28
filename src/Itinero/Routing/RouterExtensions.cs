using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Geo;
using Itinero.Geo.Directions;
using Itinero.Network;
using Itinero.Snapping;

namespace Itinero.Routing
{
    internal static class RouterExtensions
    {
        internal static (SnapPoint snapPoint, bool? direction) ToDirected(
            this (SnapPoint snapPoint, DirectionEnum? angle) snapPointAndDirection, RoutingNetwork routerDb)
        {
            if (snapPointAndDirection.angle == null) return (snapPointAndDirection.snapPoint, null);
            return (snapPointAndDirection.snapPoint,
                snapPointAndDirection.snapPoint.DirectionFromAngle(routerDb, (double)snapPointAndDirection.angle, out _));
        }
        
        internal static (SnapPoint snapPoint, bool? direction) ToDirected(
            this (SnapPoint snapPoint, double angle) snapPointAndDirection, RoutingNetwork routerDb)
        {
            return (snapPointAndDirection.snapPoint,
                snapPointAndDirection.snapPoint.DirectionFromAngle(routerDb, snapPointAndDirection.angle, out _));
        }
        
        internal static IReadOnlyList<(SnapPoint sp, bool? directed)> ToDirected(this IEnumerable<(SnapPoint snapPoint, DirectionEnum? directionEnum)> sps, 
            RoutingNetwork routerDb)
        {
            var directedSps = new List<(SnapPoint sp, bool? directed)>();
            foreach (var sp in sps)
            {
                directedSps.Add((sp.snapPoint, (double)sp.directionEnum).ToDirected(routerDb));
            }

            return directedSps;
        }
        
        internal static IReadOnlyList<(SnapPoint sp, bool? directed)> ToDirected(this IReadOnlyList<(SnapPoint snapPoint, double angle)> sps, 
            RoutingNetwork routerDb)
        {
            var directedSps = new List<(SnapPoint sp, bool? directed)>();
            foreach (var sp in sps)
            {
                directedSps.Add(sp.ToDirected(routerDb));
            }

            return directedSps;
        }
        
        internal static IReadOnlyList<(SnapPoint sp, bool? directed)> ToDirected(this IReadOnlyList<SnapPoint> sps)
        {
            var directedSps = new List<(SnapPoint sp, bool? directed)>();
            foreach (var sp in sps)
            {
                directedSps.Add((sp, null));
            }

            return directedSps;
        }

        internal static IReadOnlyList<SnapPoint> ToUndirected(
            this IReadOnlyList<(SnapPoint sp, bool? directed)> directedSps)
        {
            var sps = new List<SnapPoint>();
            foreach (var (sp, direction) in directedSps)
            {
                if (direction != null) throw new InvalidDataException($"{nameof(SnapPoint)} is directed cannot convert to undirected.");
                sps.Add(sp);
            }

            return sps;
        }

        internal static bool TryToUndirected(
            this IEnumerable<(SnapPoint sp, bool? directed)> directedSps, out IReadOnlyList<SnapPoint> undirected)
        {            
            var sps = new List<SnapPoint>();
            foreach (var (sp, direction) in directedSps)
            {
                if (direction != null)
                {
                    sps.Clear();
                    undirected = sps;
                    return false;
                }
                sps.Add(sp);
            }

            undirected = sps;
            return true;
        }

        internal static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)?
            MaxBoxFor(this RoutingSettings settings,
                RoutingNetwork routerDb, IEnumerable<(SnapPoint sp, bool? direction)> sps)
        {
            return settings.MaxBoxFor(routerDb, sps.Select(x => x.sp));
        }

        internal static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? MaxBoxFor(this RoutingSettings settings, 
            RoutingNetwork routerDb, IEnumerable<SnapPoint> sp)
        {
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? maxBox =
                null;

            if (!(settings.MaxDistance < double.MaxValue)) return null;
            
            foreach (var source in sp)
            {
                var sourceLocation = source.LocationOnNetwork(routerDb);
                var sourceBox = sourceLocation.BoxAround(settings.MaxDistance);
                maxBox = maxBox?.Expand(sourceBox) ?? sourceBox;
            }

            return maxBox;
        }
    }
}