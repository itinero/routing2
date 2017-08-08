using OsmSharp.Streams;
using System;
using System.Collections.Generic;
using System.IO;

namespace Itinero.Tiled.Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };

            var zoom = uint.Parse(args[0]);
            var zoomSource = uint.Parse(args[1]);
            var zoomDir = Path.Combine(args[2], zoomSource.ToInvariantString());

            foreach (var xDir in Directory.EnumerateDirectories(zoomDir))
            {
                var xDirInfo = new DirectoryInfo(xDir);
                var x = uint.Parse(xDirInfo.Name);
                foreach (var yFile in Directory.EnumerateFiles(xDir, "*.osm.pbf"))
                {
                    var yFileInfo = new FileInfo(yFile);
                    var y = uint.Parse(yFileInfo.Name.Substring(0, yFileInfo.Name.IndexOf(".")));

                    var tile = new Itinero.Tiled.Tiles.Tile()
                    {
                        X = x,
                        Y = y,
                        Zoom = zoomSource
                    };

                    Console.WriteLine("File found: {0}: {1} - processing", yFile, tile.LocalId);

                    using (var stream = File.OpenRead(yFile))
                    {
                        var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);

                        var tileIds = new HashSet<ulong>();
                        foreach(var st in tile.GetSubtilesAt(zoom))
                        {
                            tileIds.Add(st.LocalId);
                        }

                        Process(source, tileIds, args[3]);
                    }
                }
            }
        }

        static void Process(OsmStreamSource source, HashSet<ulong> tileIds, string outputPath)
        {
            var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            progress.RegisterSource(source);

            var db = new RouterDbTiled(11);
            var target = new IO.RouterDbTiledStreamTarget(db, new Profiles.Vehicle[] {
                Itinero.Osm.Vehicles.Vehicle.Car,
                Itinero.Osm.Vehicles.Vehicle.Bicycle,
                Itinero.Osm.Vehicles.Vehicle.Pedestrian
            });
            target.RegisterSource(progress);
            target.Pull();
            
            foreach (var tileId in db.Graph.TileIds)
            {
                if (tileIds.Contains(tileId))
                {
                    var tile = db.Graph.GetTile(tileId);
                    var tileObject = Itinero.Tiled.Tiles.Tile.FromLocalId(db.Graph.Zoom, tileId);

                    var outputFile = Path.Combine(outputPath, tileObject.Zoom.ToInvariantString(),
                        tileObject.X.ToInvariantString(), tileObject.Y.ToInvariantString() + ".geojson");
                    var outputFileInfo = new FileInfo(outputFile);
                    if (!outputFileInfo.Directory.Exists)
                    {
                        outputFileInfo.Directory.Create();
                    }
                    using (var stream = outputFileInfo.OpenWrite())
                    using (var textStream = new StreamWriter(stream))
                    {
                        db.WriteGeoJson(textStream, tileId, true, false, true);
                    }

                    Console.WriteLine("Tile found: {0}", tileId);
                }
            }
        }
    }
}