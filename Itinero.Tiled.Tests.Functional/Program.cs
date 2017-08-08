using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Itinero.Tiled.Tests.Functional
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
            
            var tiles = new List<Tiles.Tile>();
            var finalZoom = 7;
            tiles.Add(new Tiles.Tile()
            {
                X = 0,
                Y = 0,
                Zoom = 0
            });
            while (tiles.Count > 0)
            {
                var parent = tiles[0];
                tiles.RemoveAt(0);

                var osmosisCommand = @"osmosis --read-pbf {0} --tee 4 ";
                var osmosisComandTile = "--bp file={0} completeWays=yes --write-pbf {1} ";

                var batFileName = Path.Combine(parent.Zoom.ToInvariantString(), parent.X.ToInvariantString(),
                    parent.Y.ToInvariantString() + "-split.bat");
                var fileName = Path.Combine(parent.Zoom.ToInvariantString(), parent.X.ToInvariantString(),
                    parent.Y.ToInvariantString() + ".osm.pbf");
                var fileInfo = new FileInfo(batFileName);
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                var osmosis = string.Format(osmosisCommand, fileName);
                foreach (var tile in parent.Subtiles)
                {
                    fileName = Path.Combine(tile.Zoom.ToInvariantString(), tile.X.ToInvariantString(),
                        tile.Y.ToInvariantString() + ".poly");
                    fileInfo = new FileInfo(fileName);
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                    }
                    
                    var pbfFileName = Path.Combine(tile.Zoom.ToInvariantString(), tile.X.ToInvariantString(),
                        tile.Y.ToInvariantString() + ".osm.pbf");
                    using (var stream = File.OpenWrite(fileName))
                    using (var textStream = new StreamWriter(stream))
                    {
                        tile.ToPoly(textStream);
                    }

                    osmosis += string.Format(osmosisComandTile, fileName, pbfFileName);

                    if (tile.Zoom < finalZoom)
                    {
                        tiles.Add(tile);
                    }
                }

                File.WriteAllText(batFileName, osmosis + Environment.NewLine + "exit /B");
            }

            tiles.Add(new Tiles.Tile()
            {
                X = 0,
                Y = 0,
                Zoom = 0
            });
            for (uint zoom = 1; zoom < 7; zoom++)
            {
                var splitBat = new StringBuilder();
                foreach (var tile in tiles[0].GetSubtilesAt(zoom))
                {
                    splitBat.AppendLine(string.Format(@"call {0}\{1}\{2}-split.bat",
                        tile.Zoom.ToInvariantString(), tile.X.ToInvariantString(), tile.Y.ToInvariantString()));
                }
                File.WriteAllText(string.Format("split-{0}.bat", zoom), splitBat.ToInvariantString());
            }

            //var source = new OsmSharp.Streams.PBFOsmStreamSource(File.OpenRead(@"C:\work\data\OSM\belgium-latest.osm.pbf"));

            //var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            //progress.RegisterSource(source);

            //var db = new RouterDbTiled(11);
            //var target = new IO.RouterDbTiledStreamTarget(db, new Profiles.Vehicle[] {
            //    Itinero.Osm.Vehicles.Vehicle.Car,
            //    Itinero.Osm.Vehicles.Vehicle.Bicycle,
            //    Itinero.Osm.Vehicles.Vehicle.Pedestrian
            //});
            //target.RegisterSource(progress);
            //target.Pull();

            //var count = 0;
            //foreach (var tileId in db.Graph.TileIds)
            //{
            //    count++;
            //    var tile = db.Graph.GetTile(tileId);

            //    var json = db.GetGeoJson(tileId, true, false, true);
            //    File.WriteAllText(string.Format("{0}.geojson", tileId), json);

            //    Console.WriteLine("Tile found: {0}", tileId);
            //}

            //Console.WriteLine("{0} tiles found!", count);
            //Console.WriteLine("Done!");
            //Console.ReadLine();
        }
    }
}