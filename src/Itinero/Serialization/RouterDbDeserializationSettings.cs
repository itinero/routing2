using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Profiles.Serialization;

namespace Itinero.Serialization
{
    /// <summary>
    /// Router db deserialization settings.
    /// </summary>
    public sealed class RouterDbDeserializationSettings
    {
        private readonly Dictionary<string, Action<RouterDb, Stream>> _hooks = 
            new Dictionary<string, Action<RouterDb, Stream>>();
        
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string? Path { get; set; }
        
        /// <summary>
        /// Gets or sets the profile serializer.
        /// </summary>
        public IProfileSerializer ProfileSerializer { get; set; } = new DefaultProfileSerializer();

        /// <summary>
        /// Adds a deserialization hook.
        /// </summary>
        /// <param name="name">The name of the hook.</param>
        /// <param name="hook">The hook.</param>
        public void Use(string name, Action<RouterDb, Stream> hook)
        {
            _hooks[name] = hook;
        }

        internal bool TryGetHook(string name, out Action<RouterDb, Stream> hook)
        {
            return _hooks.TryGetValue(name, out hook);
        }
    }
}