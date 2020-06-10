using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data;
using Itinero.Data.Graphs.Serialization;
using Itinero.IO;
using Itinero.Profiles;
using Itinero.Profiles.EdgeTypes;
using Itinero.Serialization;

namespace Itinero
{
    public sealed partial class RouterDb : ISerializableRouterDb
    {
        private readonly Dictionary<string, Action<Stream>> _serializationHooks = new Dictionary<string, Action<Stream>>();

        void ISerializableRouterDb.AddSerializationHook(string name, Action<Stream> stream)
        {
            _serializationHooks[name] = stream;
        }
        
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
            
            // write profile configuration.
            this.ProfileConfiguration.WriteTo(stream, settings.ProfileSerializer);
            
            // get mutable network and serialize.
            using var mutableNetwork = this.Network.GetAsMutable();
            stream.WriteGraph(mutableNetwork.Graph);
            
            // run all serialization hooks.
            stream.WriteVarInt32(_serializationHooks.Count);
            foreach (var hook in _serializationHooks)
            {
                stream.WriteWithSize(hook.Key);
                hook.Value(new LimitedStream(stream));
            }
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
            
            // read profile configuration.
            var profileConfiguration = RouterDbProfileConfiguration.ReadFrom(stream, settings.ProfileSerializer);
            
            // deserialize network.
            var graph = stream.ReadGraph(attributes => 
                profileConfiguration.Profiles.GetEdgeProfileFor(attributes));
            
            var routerDb = new RouterDb(graph) {ProfileConfiguration = profileConfiguration};
            
            // run all deserialization hooks.
            var hookCount = stream.ReadVarInt32();
            for (var h = 0; h < hookCount; h++)
            {
                var key = stream.ReadWithSizeString();
                if (!settings.TryGetHook(key, out var hook)) continue;

                hook(routerDb, new LimitedStream(stream));
            }

            return routerDb;
        }
    }

    /// <summary>
    /// Abstract representation of a serializable router db.
    /// </summary>
    public interface ISerializableRouterDb
    {
        /// <summary>
        /// Adds a serialization hook.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The stream.</param>
        public void AddSerializationHook(string name, Action<Stream> stream);
    }
}