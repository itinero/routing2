using System;
using System.Threading.Tasks;
using Itinero.MapMatching.Model;
using Itinero.Profiles;
using Xunit;

namespace Itinero.MapMatching.Tests;

public class ModelBuilderTests
{
    [Fact]
    public async Task ModelBuilder_OneHopTrack()
    {
        var track = new Track(new[]
        {
            new TrackPoint(4.784586882060637,
                51.27024704344623),
            new TrackPoint(4.785115480803853,
                51.27048872037136)
        });

        var routerDb = new RouterDb();
        var writer = routerDb.Latest.GetWriter();
        var vertex1 = writer.AddVertex(
            4.784586882060637,
            51.27024704344623);
        var vertex2 = writer.AddVertex(4.785115480803853,
            51.27048872037136);
        writer.AddEdge(vertex1, vertex2);
        writer.Dispose();

        var modelBuilder = new ModelBuilder(routerDb.Latest);
        var models = await modelBuilder.BuildModels(track, new DefaultProfile());


    }
}
