using System;
using System.Collections.Generic;
using System.IO;

namespace Itinero.Serialization
{
    /// <summary>
    /// Router db deserialization settings.
    /// </summary>
    public sealed class RouterDbDeserializationSettings
    {
        private readonly Dictionary<string, (Action<Stream> deserialize, Action<RouterDb> apply)> _hooks;

        public RouterDbDeserializationSettings()
        {
            _hooks = new Dictionary<string, (Action<Stream> deserialize, Action<RouterDb> apply)>();
        }

        /// <summary>
        /// Registers a deserialization hook.
        /// </summary>
        /// <param name="key">The key of the hook.</param>
        /// <param name="deserialize">The deserialization action.</param>
        /// <param name="apply">The apply action.</param>
        public void RegisterHook(string key, Action<Stream> deserialize, Action<RouterDb> apply)
        {
            _hooks.Add(key, (deserialize, apply));
        }
        
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string? Path { get; set; }

        internal bool TryGetHook(string key, out (Action<Stream> deserialize, Action<RouterDb> apply) hook)
        {
            return _hooks.TryGetValue(key, out hook);
        }
    }
}