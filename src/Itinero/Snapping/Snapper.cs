using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Search;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    internal class Snapper : ISnapper
    {
        public Snapper(RoutingNetwork routingNetwork,
            SnapperSettings? settings = null)
        {
            this.RoutingNetwork = routingNetwork;
            this.Settings = settings ?? new SnapperSettings();
        }

        internal RoutingNetwork RoutingNetwork { get; }

        internal SnapperSettings Settings { get; }

        /// <inheritdoc/>
        public ILocationsSnapper Using(Action<SnapperSettings> settings)
        {
            var s = new SnapperSettings();
            settings?.Invoke(s);

            return new Snapper(this.RoutingNetwork, s);
        }

        /// <inheritdoc/>
        public ILocationsSnapper Using(Profile profile, Action<SnapperSettings>? settings = null)
        {
            var s = new SnapperSettings();
            settings?.Invoke(s);

            return new LocationsSnapper(this, new[] { profile })
            {
                AnyProfile = s.AnyProfile,
                CheckCanStopOn = s.CheckCanStopOn,
                OffsetInMeter = s.OffsetInMeter,
                OffsetInMeterMax = s.OffsetInMeterMax
            };
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<Result<SnapPoint>> ToAsync(IEnumerable<(VertexId vertexId, EdgeId? edgeId)> vertices,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var enumerator = this.RoutingNetwork.GetEdgeEnumerator();

            foreach (var (vertexId, edgeId) in vertices)
            {
                if (!enumerator.MoveTo(vertexId))
                {
                    yield return new Result<SnapPoint>($"Vertex {vertexId} not found.");
                    continue;
                }

                var found = false;
                while (enumerator.MoveNext())
                {
                    if (edgeId != null &&
                        enumerator.EdgeId != edgeId.Value)
                    {
                        continue;
                    }

                    if (enumerator.Forward)
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, 0));
                    }
                    else
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.EdgeId, ushort.MaxValue));
                    }

                    found = true;
                    break;
                }

                if (found)
                {
                    continue;
                }

                if (edgeId.HasValue)
                {
                    yield return new Result<SnapPoint>($"Edge {edgeId.Value} not found for vertex {vertexId}");
                }
                else
                {
                    yield return new Result<SnapPoint>("Cannot snap to a vertex that has no edges.");
                }
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<Result<SnapPoint>> ToAsync(IEnumerable<(double longitude, double latitude, float? e)> locations,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var location in locations)
            {
                // calculate search box.
                var box = location.BoxAround(this.Settings.OffsetInMeter);

                // make sure data is loaded.
                if (this.RoutingNetwork.RouterDb?.UsageNotifier != null) await this.RoutingNetwork.RouterDb.UsageNotifier.NotifyBox(this.RoutingNetwork, box, cancellationToken);

                // break when cancelled.
                if (cancellationToken.IsCancellationRequested) break;

                // snap to closest edge.
                var snapPoint = this.RoutingNetwork.SnapInBox(box, (_) => true);
                if (snapPoint.EdgeId != EdgeId.Empty)
                {
                    yield return snapPoint;
                }
                else
                {
                    yield return new Result<SnapPoint>(
                        $"Could not snap to location: {location.longitude},{location.latitude}");
                }
            }
        }
    }
}
