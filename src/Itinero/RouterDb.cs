using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Indexes;
using Itinero.IO;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Network.Serialization;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero
{
    /// <summary>
    /// Represents a router db.
    /// </summary>
    /// <remarks>
    /// This is mostly a wrapper around a RoutingNetwork.
    /// If an update happens to the underlying routing network, this update will be presented here atomically.
    /// In other words, to do route planning, use routerDb.getLatest() to get a consistent routing network
    /// </remarks>
    public sealed partial class RouterDb : IRouterDbMutable
    {
        /// <summary>
        /// Creates a new router db.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public RouterDb(RouterDbConfiguration? configuration = null)
        {
            configuration ??= RouterDbConfiguration.Default;

            this.Latest = new RoutingNetwork(this, configuration.Zoom);
            _edgeTypeIndex = new AttributeSetIndex();
            _edgeTypeMap = AttributeSetMap.Default;
            _turnCostTypeIndex = new AttributeSetIndex();
            _turnCostTypeMap = AttributeSetMap.Default;

            this.ProfileConfiguration = new RouterDbProfileConfiguration(this);
        }

        private RouterDb(Stream stream)
        {
            // check version #.
            var version = stream.ReadVarInt32();
            if (version != 1) {
                throw new InvalidDataException("Unknown version #.");
            }

            // read network.
            this.Latest = stream.ReadFrom(this);

            // read edge type map data.
            _edgeTypeIndex = AttributeSetIndex.ReadFrom(stream);
            _turnCostTypeIndex = AttributeSetIndex.ReadFrom(stream);
            
            // read attributes.
            this.Meta = new List<(string key, string value)>(ReadAttributesFrom(stream));

            _edgeTypeMap = AttributeSetMap.Default;
            _turnCostTypeMap = AttributeSetMap.Default;
            this.ProfileConfiguration = new RouterDbProfileConfiguration(this);
        }

        /// <summary>
        /// Gets the latest.
        /// </summary>
        public RoutingNetwork Latest { get; private set; }

        /// <summary>
        /// Gets the usage notifier.
        /// </summary>
        public DataUseNotifier UsageNotifier { get; } = new();
    }
}