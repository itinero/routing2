using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Logging;
using Newtonsoft.Json.Linq;

namespace Itinero.IO.Osm.Tiles.Parsers.Semantics;

public static class TagMapperConfigParser
{
    public static Dictionary<string, TagMapperConfig> Parse(Stream stream)
    {
        var mappings = new Dictionary<string, TagMapperConfig>();

        using (var textReader = new StreamReader(stream))
        {
            var parsed = JArray.Parse(textReader.ReadToEnd());

            foreach (var item in parsed)
            {
                try
                {
                    var osmKeyValue = item["osm_key"];
                    if (osmKeyValue == null)
                    {
                        throw new Exception("osm_key not found.");
                    }

                    if (osmKeyValue.Type != JTokenType.String)
                    {
                        throw new Exception("osm_key not a string.");
                    }

                    var osmKey = osmKeyValue.Value<string>();
                    var predicateValue = item["predicate"];
                    if (predicateValue == null)
                    {
                        throw new Exception("predicate not found.");
                    }

                    if (predicateValue.Type != JTokenType.String)
                    {
                        throw new Exception("predicate not a string.");
                    }

                    var predicate = predicateValue.Value<string>();

                    var map = item["mapping"];
                    Dictionary<string, string>? mapping = null;
                    if (map != null)
                    {
                        mapping = new Dictionary<string, string>();
                        foreach (var child in map.Children())
                        {
                            if (!(child is JProperty property))
                            {
                                continue;
                            }

                            if (property.Value is JValue val)
                            {
                                mapping[val.Value.ToInvariantString()] = property.Name;
                            }
                        }
                    }

                    mappings[predicate] = new TagMapperConfig
                    {
                        ReverseMapping = mapping,
                        OsmKey = osmKey,
                        Predicate = predicate
                    };
                }
                catch (Exception ex)
                {
                    Logger.Log($"{nameof(TagMapperConfigParser)}.{nameof(Parse)}", TraceEventType.Error,
                        "Could not fully parse mapping configuration {0}", ex);
                    throw;
                }
            }
        }

        return mappings;
    }
}
