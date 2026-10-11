using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Snapping;

namespace Itinero.Routing.Isochrones;

/// <summary>
/// Areas reachable from a point within given costs.
/// </summary>
/// <remarks>
/// One expansion, cut just past the outermost band, rasterised onto a grid and contoured. The
/// grid rather than a hull over reached vertices: edges carry shape, and a hull drawn between
/// endpoints would cut the corners off every long link.
/// </remarks>
public static class IsochroneSearch
{
    /// <summary>
    /// Computes the areas reachable from a snapped point at each of the given costs.
    /// </summary>
    /// <param name="bands">Cost bounds in the cost function's units. Order does not matter.</param>
    /// <returns>The bands, innermost first.</returns>
    public static async Task<Result<Isochrone>> IsochroneAsync(
        this RoutingNetwork network,
        RoutingSettings settings,
        SnapPoint source,
        IReadOnlyList<double> bands,
        IsochroneSettings? isochroneSettings = null,
        CancellationToken cancellationToken = default)
    {
        if (bands.Count == 0) throw new ArgumentException("No bands given.", nameof(bands));
        foreach (var band in bands)
        {
            if (band <= 0) throw new ArgumentOutOfRangeException(nameof(bands),
                "A band has to cost more than nothing.");
        }

        var options = isochroneSettings ?? new IsochroneSettings();
        options.Validate();

        var ordered = bands.Distinct().OrderBy(x => x).ToList();
        var outer = ordered[^1];
        // Searched past the outermost band so the contour has something on both sides of it to
        // interpolate between. Without it the boundary pulls in to the last reached vertex.
        var cut = outer * (1.0 + options.Margin);

        var profile = settings.Profile;
        var costFunction = network.GetCostFunctionFor(profile);
        if (settings.CostFunctionWrapper != null) costFunction = settings.CostFunctionWrapper(costFunction);

        var reached = new Dictionary<VertexId, double>();
        var settled = 0L;

        var search = SearchPool<Dijkstra>.Rent();
        try
        {
            await search.RunAsync(network, (source, (bool?)null),
                [], costFunction.GetDijkstraWeightFunc(),
                async state =>
                {
                    if (!network.UsageNotifier.IsVertexDataReady(network, state.vertexId))
                    {
                        await network.UsageNotifier.NotifyVertex(network, state.vertexId, cancellationToken);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    settled++;

                    // Cheapest arrival wins: the same vertex is settled once per incoming edge
                    // in an edge-based search, and the surface wants the cost of being there.
                    if (!reached.TryGetValue(state.vertexId, out var best) || state.cost < best)
                    {
                        reached[state.vertexId] = state.cost;
                    }

                    // True stops expansion from this state, which is what bounds the search.
                    return state.cost > cut;
                },
                cancellationToken: cancellationToken,
                isMainN: network.GetIsMainNFunc(profile));
        }
        finally
        {
            SearchPool<Dijkstra>.Return(search);
        }

        if (reached.Count == 0)
            return new Result<Isochrone>("Nothing reachable from the origin.");

        var grid = BuildSurface(network, reached, options, cancellationToken, out var diagnostics);
        diagnostics = diagnostics with { Settled = settled, Reached = reached.Count };

        var result = new List<IsochroneBand>(ordered.Count);
        foreach (var band in ordered)
        {
            result.Add(new IsochroneBand(band, ContourTracer.Trace(grid.Cost, grid.Width,
                grid.Height, band, grid.MinLon, grid.MinLat, grid.LonPerCell, grid.LatPerCell,
                options.Denoise, grid.SquareMetersPerSquareDegree,
                options.FillHolesBelowSquareMeters, cancellationToken)));
        }

        return new Isochrone(result, diagnostics);
    }

    private readonly record struct Surface(
        double[] Cost, int Width, int Height,
        double MinLon, double MinLat, double LonPerCell, double LatPerCell,
        double SquareMetersPerSquareDegree);

    /// <summary>
    /// Stamps the cost of every reached edge onto a grid, interpolating along the edge's shape.
    /// </summary>
    private static Surface BuildSurface(RoutingNetwork network,
        Dictionary<VertexId, double> reached, IsochroneSettings options,
        CancellationToken cancellationToken, out IsochroneDiagnostics diagnostics)
    {
        var minLon = double.MaxValue;
        var minLat = double.MaxValue;
        var maxLon = double.MinValue;
        var maxLat = double.MinValue;
        foreach (var vertex in reached.Keys)
        {
            var (longitude, latitude, _) = network.GetVertex(vertex);
            minLon = Math.Min(minLon, longitude);
            minLat = Math.Min(minLat, latitude);
            maxLon = Math.Max(maxLon, longitude);
            maxLat = Math.Max(maxLat, latitude);
        }

        var centreLat = (minLat + maxLat) / 2.0;

        // Scaled to the area actually reached when not given, so one setting serves a short walk
        // and an hour's drive. Valhalla sizes its grid the same way.
        var cellSize = options.CellSizeMeters ?? 0;
        if (cellSize <= 0)
        {
            var southWest = (minLon, minLat, (float?)null);
            var across = Math.Max(
                southWest.DistanceEstimateInMeter((maxLon, minLat, (float?)null)),
                southWest.DistanceEstimateInMeter((minLon, maxLat, (float?)null)));
            cellSize = Math.Max(options.MinCellSizeMeters, across / options.TargetCellsAcross);
        }

        var (lonPerCell, latPerCell) = CellInDegrees(cellSize, centreLat);

        var width = (int)Math.Ceiling((maxLon - minLon) / lonPerCell) + 1;
        var height = (int)Math.Ceiling((maxLat - minLat) / latPerCell) + 1;

        // Coarsen rather than allocate without bound: a wide band over a sparse network would
        // otherwise size the grid off its own extent.
        if ((long)width * height > options.MaxCells)
        {
            var factor = Math.Sqrt((double)width * height / options.MaxCells);
            cellSize *= factor;
            (lonPerCell, latPerCell) = CellInDegrees(cellSize, centreLat);
            width = (int)Math.Ceiling((maxLon - minLon) / lonPerCell) + 1;
            height = (int)Math.Ceiling((maxLat - minLat) / latPerCell) + 1;
        }

        // Padded by the reach, so a dilated band never runs into the edge of the grid and
        // leaves a ring that cannot close.
        var pad = (int)Math.Round(options.ReachRadiusMeters / cellSize) + 2;
        width += pad * 2;
        height += pad * 2;
        minLon -= lonPerCell * pad;
        minLat -= latPerCell * pad;

        var cost = new double[(long)width * height];
        Array.Fill(cost, double.MaxValue);

        var enumerator = network.GetEdgeEnumerator();
        var seen = new HashSet<EdgeId>();
        var stamped = 0;
        foreach (var vertex in reached.Keys)
        {
            // One pass per reached vertex, each walking every incident edge's shape.
            if ((stamped++ & 0xFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (!enumerator.MoveTo(vertex)) continue;
            while (enumerator.MoveNext())
            {
                if (!seen.Add(enumerator.EdgeId)) continue;
                if (!reached.TryGetValue(enumerator.Tail, out var tailCost)) continue;
                if (!reached.TryGetValue(enumerator.Head, out var headCost)) continue;

                Stamp(enumerator, tailCost, headCost);
            }
        }

        Dilate(cost, width, height, (int)Math.Round(options.ReachRadiusMeters / cellSize),
            cancellationToken);

        diagnostics = new IsochroneDiagnostics(0, 0, (long)width * height, cellSize);
        // One square degree in square metres here, so a hole's area can be judged in metres.
        var metersPerDegreeLon = cellSize / lonPerCell;
        var metersPerDegreeLat = cellSize / latPerCell;
        return new Surface(cost, width, height, minLon, minLat, lonPerCell, latPerCell,
            metersPerDegreeLon * metersPerDegreeLat);

        void Stamp(RoutingNetworkEdgeEnumerator edge, double tailCost, double headCost)
        {
            var shape = edge.GetCompleteShape().ToList();
            if (shape.Count < 2) return;

            var lengths = new double[shape.Count];
            var total = 0d;
            for (var i = 1; i < shape.Count; i++)
            {
                total += shape[i - 1].DistanceEstimateInMeter(shape[i]);
                lengths[i] = total;
            }

            for (var i = 0; i < shape.Count; i++)
            {
                var at = total <= 0 ? 0 : lengths[i] / total;
                var here = tailCost + (headCost - tailCost) * at;
                Mark(shape[i].longitude, shape[i].latitude, here);

                if (i + 1 >= shape.Count) continue;

                // Sample between shape points too: a cell the edge merely crosses must be
                // marked, or a straight link longer than a cell leaves gaps in the surface.
                var span = lengths[i + 1] - lengths[i];
                var steps = (int)Math.Floor(span / cellSize);
                for (var s = 1; s <= steps; s++)
                {
                    var f = s / (double)(steps + 1);
                    var lon = shape[i].longitude + (shape[i + 1].longitude - shape[i].longitude) * f;
                    var lat = shape[i].latitude + (shape[i + 1].latitude - shape[i].latitude) * f;
                    var mid = total <= 0
                        ? here
                        : tailCost + (headCost - tailCost) * ((lengths[i] + span * f) / total);
                    Mark(lon, lat, mid);
                }
            }
        }

        void Mark(double longitude, double latitude, double value)
        {
            var x = (int)Math.Round((longitude - minLon) / lonPerCell);
            var y = (int)Math.Round((latitude - minLat) / latPerCell);
            if (x < 0 || y < 0 || x >= width || y >= height) return;

            var index = (long)y * width + x;
            if (value < cost[index]) cost[index] = value;
        }
    }

    /// <summary>
    /// Spreads each cell's cost to those within <paramref name="radius"/> cells of it.
    /// </summary>
    /// <remarks>
    /// Separable: the minimum over a square is the minimum along each row, then down each
    /// column. Taking the minimum is what makes it mean "the cheapest road within reach".
    /// </remarks>
    private static void Dilate(double[] cost, int width, int height, int radius,
        CancellationToken cancellationToken = default)
    {
        if (radius < 1) return;

        var row = new double[width];
        for (var y = 0; y < height; y++)
        {
            // Per row rather than per cell: the grid runs to millions of cells.
            cancellationToken.ThrowIfCancellationRequested();
            var offset = (long)y * width;
            for (var x = 0; x < width; x++) row[x] = cost[offset + x];
            for (var x = 0; x < width; x++)
            {
                var best = double.MaxValue;
                var from = Math.Max(0, x - radius);
                var to = Math.Min(width - 1, x + radius);
                for (var i = from; i <= to; i++) best = Math.Min(best, row[i]);
                cost[offset + x] = best;
            }
        }

        var column = new double[height];
        for (var x = 0; x < width; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var y = 0; y < height; y++) column[y] = cost[(long)y * width + x];
            for (var y = 0; y < height; y++)
            {
                var best = double.MaxValue;
                var from = Math.Max(0, y - radius);
                var to = Math.Min(height - 1, y + radius);
                for (var i = from; i <= to; i++) best = Math.Min(best, column[i]);
                cost[(long)y * width + x] = best;
            }
        }
    }

    /// <summary>
    /// Cell size in degrees. Taken once at the centre latitude rather than per point: the
    /// longitude scale varies across a band, but not enough to matter at a cell's size.
    /// </summary>
    private static (double lonPerCell, double latPerCell) CellInDegrees(double meters, double latitude)
    {
        const double metersPerDegreeLat = 111_320.0;
        var latPerCell = meters / metersPerDegreeLat;
        var cos = Math.Cos(latitude * Math.PI / 180.0);
        var lonPerCell = Math.Abs(cos) < 1e-6
            ? latPerCell
            : meters / (metersPerDegreeLat * cos);
        return (lonPerCell, latPerCell);
    }
}
