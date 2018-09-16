using System;
using System.IO;
using Itinero.Data.Tiles;
using OsmSharp;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var stream = File.OpenRead(@"/home/xivk/work/data/OSM/brussels.osm.pbf"))
            {
                var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);
                foreach (var osmGeo in source)
                {
                    if (!(osmGeo is Node node)) break;

                    var tile = Tile.WorldToTile(node.Latitude.Value, node.Longitude.Value, 14);
                    var localCoordinates = tile.ToLocalCoordinates(node.Latitude.Value, node.Longitude.Value, 4096);
                    var globalCoordinates = tile.FromLocalCoordinates(localCoordinates.x, localCoordinates.y, 4096);
                    
                    
                }
            }
        }
    }
}
