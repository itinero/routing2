using Itinero.Profiles.Serialization;

namespace Itinero.Serialization
{
    /// <summary>
    /// Router db serialization settings.
    /// </summary>
    public class RouterDbSerializationSettings
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string? Path { get; set; }
        
        /// <summary>
        /// Gets or sets the profile serializer.
        /// </summary>
        public IProfileSerializer ProfileSerializer { get; set; } = new DefaultProfileSerializer();
    }
}