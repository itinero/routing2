using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Serialization;

namespace Itinero
{
    public sealed partial class RouterDb
    {
        private readonly List<Action<Stream>> _serializationHooks = new List<Action<Stream>>();

        public void RegisterSerializationHook(Action<Stream> serializationHook)
        {
            _serializationHooks.Add(serializationHook);
        }

        public void Serialize(Action<RouterDbSerializationSettings> config)
        {
            // config settings.
            var settings = new RouterDbSerializationSettings();
            config(settings);
            
            // open stream and write.
            using (var stream = File.Open(settings.Path, FileMode.Create))
            {
                
            }
        }
    }
}