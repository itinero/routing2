using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json.Linq;

namespace Itinero.Tests.Profiles
{
    internal static class GeoJsonExtensions
    {
        public static string AddColours(this string geojson)
        {
            var parsed = JObject.Parse(geojson);
            var profiles = parsed.AllProfiles();
            Console.Write($" {profiles.Count()} profiles ");

            parsed = parsed.AddColours(profiles);

            return parsed.ToString();
        }

        private static IEnumerable<string> AllProfiles(this JObject parsed)
        {
            var features = parsed["features"].Children();

            var profiles = new HashSet<string>();

            foreach (var feature in features)
            {
                if (feature["geometry"]["type"].Value<string>().Equals("Point"))
                {
                    continue;
                }

                var props = feature["properties"];

                foreach (var kv in props)
                {
                    var prop = (JProperty) kv;
                    if (prop.Name.EndsWith("speed_corrected"))
                    {
                        profiles.Add(prop.Name);
                    }
                }
            }

            return profiles;
        }

        // Add 
        private static JObject AddColours(this JObject parsed, IEnumerable<string> profileNames)
        {
            var features = parsed["features"].Children();
            var max = new Dictionary<string, double>();
            var min = new Dictionary<string, double>();
            foreach (var feature in features)
            {
                if (feature["geometry"]["type"].Value<string>().Equals("Point"))
                {
                    continue;
                }

                var props = feature["properties"];

                foreach (var profileName in profileNames)
                {
                    if (!max.ContainsKey(profileName))
                    {
                        max[profileName] = double.MinValue;
                        min[profileName] = double.MaxValue;
                    }

                    var speed = props[profileName].Value<double>();
                    if ((int) speed == 65536)
                    {
                        continue;
                    }
                    
                    
                    max[profileName] = Math.Max(max[profileName], speed);
                    min[profileName] = Math.Min(min[profileName], speed);
                }
            }
            

            foreach (var feature in features)
            {
                if (feature["geometry"]["type"].Value<string>().Equals("Point"))
                {
                    continue;
                }

                foreach (var profileName in profileNames)
                {
                    var props = feature["properties"];

                    var speed = props[profileName].Value<double>();
                    
                    
                    
                    if ((int) speed == 65536)
                    {
                        props["colour_" + profileName] =
                            $"#ffff0000"; // First ff is opacity, must always be ff for qgis
                        continue;

                    }
                    var speedNormalized = (speed - min[profileName]) / (max[profileName] - min[profileName]);

                    if (speedNormalized > 1.0)
                    {
                        Console.WriteLine("Normalized speed > 1");
                    }
                    
                    var colour = (1 - speedNormalized) * 230; // There is 25 buffer to show light-gray when it is not preferred
                    var hex = ToHex(Math.Min(255, (uint) colour));

                    props["colour_" + profileName] =
                        $"#ff{hex}{hex}{hex}"; // First ff is opacity, must always be ff for qgis

                    if (hex == "ff")
                    {
                        props["colour_" + profileName] =
                            $"#ff00{hex}00"; // First ff is opacity, must always be ff for qgis

                    }
                }
            }

            return parsed;
        }

        private static char[] digits = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};


        public static string ToHex(uint d)
        {
            var hex = ToHexSimple(d);
            if (hex.Length == 3 && hex.StartsWith("0"))
            {
                return hex.Substring(1);
            }
            if (hex.Length == 2)
            {
                return hex;
            }
            if (hex.Length == 1)
            {
                return "0" + hex;
            }

            return hex;
        }
        
        private static string ToHexSimple(uint d)
        {
            var i = (int) d;
            var lsd = d % 16;
            var chr = digits[lsd];
            var rest = (uint) (i / 16);
            if (rest == 0)
            {
                return "" + chr;
            }

            return ToHex(rest) + chr;
        }

        /// <summary>
        /// Converts the given feature collection to geojson.
        /// </summary>
        public static string ToJson(this Geometry geometry)
        {
            var features = new FeatureCollection();
            features.Add(new Feature(geometry, new AttributesTable()));
            return features.ToJson();
        }

        /// <summary>
        /// Converts the given feature collection to geojson.
        /// </summary>
        public static string ToJson(this FeatureCollection featureCollection)
        {
            var jsonSerializer = GeoJsonSerializer.Create();
            var jsonStream = new StringWriter();
            jsonSerializer.Serialize(jsonStream, featureCollection);
            var json = jsonStream.ToString();
            return json;
        }
    }
}