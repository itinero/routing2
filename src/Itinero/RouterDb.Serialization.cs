using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data;
using Itinero.Data.Graphs.Serialization;
using Itinero.IO;
using Itinero.Logging;
using Itinero.Serialization;

namespace Itinero
{
    public sealed partial class RouterDb
    {
        /// <summary>
        /// Serializes the router db using the given configuration.
        /// </summary>
        /// <param name="config">The config.</param>
        public void Serialize(Action<RouterDbSerializationSettings> config)
        {
            // config settings.
            var settings = new RouterDbSerializationSettings();
            config(settings);
            
            // open stream.
            using var stream = File.Open(settings.Path, FileMode.Create);
            
            // get mutable network and serialize.
            stream.WriteGraph(this.Network.GetAsMutable().Graph);
        }

        /// <summary>
        /// Deserializes the router db using the given configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>The router db.</returns>
        public static RouterDb Deserialize(Action<RouterDbDeserializationSettings> config)
        {
            // config settings.
            var settings = new RouterDbDeserializationSettings();
            config(settings);
            
            // open stream.
            using var stream = File.OpenRead(settings.Path);
            
            // deserialize network.
            var graph = stream.ReadGraph(null);
            
            return new RouterDb(graph);
        }
    }
}