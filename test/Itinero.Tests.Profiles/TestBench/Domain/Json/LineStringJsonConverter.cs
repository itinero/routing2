using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Itinero.Tests.Profiles.TestBench.Domain.Json
{
    /// <summary>
    /// A custom geometry json converter to/from GeoJSON.
    /// </summary>
    public class LineStringJsonConverter : JsonConverter
    {
        private readonly JsonSerializer _serializer = 
            NetTopologySuite.IO.GeoJsonSerializer.Create();

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsSubclassOf(typeof(LineString));
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            { // handle the special case of null, the NTS serializer can't handle this.
                return null;
            }
            return _serializer.Deserialize(reader, typeof(LineString));
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // TODO: bugfix here: https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON/issues/1
            _serializer.Serialize(writer, value);
        }
    }
}