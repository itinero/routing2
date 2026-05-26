namespace Itinero.Network.Tiles;

/// <summary>
/// Per-tile geographic extent bounds used by the tile-bounded snap algorithm
/// in <c>EdgeSearch.SnapInBoxAsync</c>. See
/// <c>src/Itinero/Network/Search/snap-algorithm.md</c> for the design.
///
/// <para>
/// <see cref="MaxLonDiff"/> and <see cref="MaxLatDiff"/> bound how far any
/// edge starting at a vertex in this tile can reach in each axis. They are
/// the maxima of (edge.bbox.maxLon − edge.bbox.minLon) and
/// (edge.bbox.maxLat − edge.bbox.minLat) over every edge with at least one
/// vertex in the tile. Cross-tile edges contribute to both endpoint tiles'
/// diffs (each tile sees them once during its own per-vertex iteration).
/// </para>
///
/// <para>
/// Computed lazily on first access via <see cref="EnsureDiffs"/>, which
/// takes a network reference so that cross-tile edges' remote endpoints
/// can be resolved to their actual coordinates. The lazy contract: the
/// snap path's <c>NotifyBox</c> has already loaded the partner tiles by
/// the time the snap algorithm asks for diffs. If a partner tile somehow
/// isn't loaded, we fall back to the partner tile's geographic bbox
/// (~tile-width-wide over-estimate) — correct but loose.
/// </para>
///
/// <para>
/// Invalidated to <see cref="double.NaN"/> whenever an edge is added or
/// removed, so late boundary edges added by neighbouring tiles'
/// <c>AddStandaloneTile</c> calls don't leave stale values.
/// </para>
/// </summary>
internal partial class NetworkTile
{
    private double _maxLonDiff = double.NaN;
    private double _maxLatDiff = double.NaN;

    /// <summary>
    /// Max lon-span across all edges with a vertex in this tile. Returns
    /// <see cref="double.NaN"/> if <see cref="EnsureDiffs"/> hasn't been
    /// called yet. Callers should invoke <see cref="EnsureDiffs"/> first.
    /// </summary>
    public double MaxLonDiff => _maxLonDiff;

    /// <summary>
    /// Max lat-span across all edges with a vertex in this tile. See
    /// <see cref="MaxLonDiff"/>.
    /// </summary>
    public double MaxLatDiff => _maxLatDiff;

    /// <summary>
    /// Reset the cached diffs. Call from any mutation that could change
    /// the tile's edge set (AddEdge, DeleteEdge, RemoveDeletedEdges).
    /// </summary>
    private void InvalidateDiffs()
    {
        _maxLonDiff = double.NaN;
        _maxLatDiff = double.NaN;
    }

    /// <summary>
    /// Compute the diffs if not already cached. Takes a network reference
    /// so cross-tile edges can resolve their remote endpoint's actual
    /// coordinate. If the partner tile isn't loaded, falls back to the
    /// partner's geographic bbox (correct but loose).
    /// </summary>
    internal void EnsureDiffs(Itinero.Network.Enumerators.Edges.IEdgeEnumerable network)
    {
        if (!double.IsNaN(_maxLonDiff)) return;

        // Walk every edge once per endpoint that's in this tile. Intra-tile
        // edges are seen twice; max is idempotent so that's fine. Cross-tile
        // edges are seen once (from the local endpoint's pointer chain),
        // which is enough — the partner tile sees them independently from
        // its own side.
        double maxLon = 0;
        double maxLat = 0;

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(this);

        for (uint v = 0; v < _nextVertexId; v++)
        {
            var vertexId = new VertexId(_tileId, v);
            if (!enumerator.MoveTo(vertexId)) continue;
            if (!this.TryGetVertex(vertexId, out var tailLon, out var tailLat, out _)) continue;

            while (enumerator.MoveNext())
            {
                var minLon = tailLon;
                var maxLonE = tailLon;
                var minLat = tailLat;
                var maxLatE = tailLat;

                // Resolve the head endpoint's actual coordinate. For
                // intra-tile edges it's right here; for cross-tile edges
                // we ask the network for the partner tile.
                var headResolved = false;
                double headLon = 0, headLat = 0;
                if (enumerator.Head.TileId == _tileId)
                {
                    if (this.TryGetVertex(enumerator.Head, out headLon, out headLat, out _))
                    {
                        headResolved = true;
                    }
                }
                else
                {
                    var partnerTile = network.GetTileForRead(enumerator.Head.TileId);
                    if (partnerTile != null &&
                        partnerTile.TryGetVertex(enumerator.Head, out headLon, out headLat, out _))
                    {
                        headResolved = true;
                    }
                }

                if (headResolved)
                {
                    if (headLon < minLon) minLon = headLon;
                    else if (headLon > maxLonE) maxLonE = headLon;
                    if (headLat < minLat) minLat = headLat;
                    else if (headLat > maxLatE) maxLatE = headLat;
                }
                else
                {
                    // Partner tile isn't loaded — fall back to its bbox.
                    // Over-estimates by up to ~one tile-width but stays
                    // correct (the per-vertex predicate built on this
                    // bound will be looser, never wrongly prune).
                    var (hMinLon, hMinLat, hMaxLon, hMaxLat) =
                        TileStatic.GetTileBoundingBox(_zoom, enumerator.Head.TileId);
                    if (hMinLon < minLon) minLon = hMinLon;
                    if (hMaxLon > maxLonE) maxLonE = hMaxLon;
                    if (hMinLat < minLat) minLat = hMinLat;
                    if (hMaxLat > maxLatE) maxLatE = hMaxLat;
                }

                // Shape points (intermediate).
                foreach (var (sLon, sLat, _) in enumerator.Shape)
                {
                    if (sLon < minLon) minLon = sLon;
                    else if (sLon > maxLonE) maxLonE = sLon;
                    if (sLat < minLat) minLat = sLat;
                    else if (sLat > maxLatE) maxLatE = sLat;
                }

                var dLon = maxLonE - minLon;
                var dLat = maxLatE - minLat;
                if (dLon > maxLon) maxLon = dLon;
                if (dLat > maxLat) maxLat = dLat;
            }
        }

        _maxLonDiff = maxLon;
        _maxLatDiff = maxLat;
    }
}
