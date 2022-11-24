using System.Linq;

namespace Itinero.Tests.Instructions;
using System.Collections.Generic;
using Itinero.Instructions;
using Itinero.Routes;
using Xunit;

public class RouteExtensionsTest
{
    
    [Fact]
    public void DirectionChangeAt_SimpleRightTurn_IsNegative()
    {
        var r = new Route
        {
            Shape = new List<(double longitude, double latitude, float? e)>
            {
                (3.2200763, 51.2159230, 0), (3.2203252, 51.215485, 0), (3.2195995, 51.215298, 0)
            },
            ShapeMeta = new List<Route.Meta>
            {
                new Route.Meta()
                {
                    Shape = 2,
                    Attributes = new List<(string key, string value)>
                    {
                        ("name","teststreet")
                    }
                }
            }
        };
        var angleDiff = new IndexedRoute(r).DirectionChangeAt(1);
        Assert.True(angleDiff < 0);
        Assert.True(-95 < angleDiff && angleDiff < -85);
    }
    [Fact]
    public void RouteExtensions_RemoveDuplicates_KeepLast_SimpleRoute_CleanedShapeMeta(){
        var r = new Route
        {
            Shape = new List<(double longitude, double latitude, float? e)>
            {
                (3.2200763, 51.2159230, 0), (3.2203252, 51.215485, 0), (3.2195995, 51.215298, 0)
            },
            ShapeMeta = new List<Route.Meta>
            {new Route.Meta()
                {
                    Shape = 2,
                    Attributes = new List<(string key, string value)>
                    {
                        ("ref","0")
                    }
                },
                new Route.Meta()
                {
                    Shape = 2,
                    Attributes = new List<(string key, string value)>
                    {
                        ("ref","1")
                    }
                }
            }
        };
        r.RemoveDuplicateShapeMeta();
        Assert.Single(r.ShapeMeta);
       
        Assert.Equal("0",  r.ShapeMeta[0].Attributes.ToList()[0].value);
    }
    
    
    [Fact]
    public void RouteExtensions_RemoveDuplicates_SimpleRoute_CleanedShapeMeta(){
        var r = new Route
        {
            Shape = new List<(double longitude, double latitude, float? e)>
            {
                (3.2200763, 51.2159230, 0), (3.2203252, 51.215485, 0), (3.2195995, 51.215298, 0)
            },
            ShapeMeta = new List<Route.Meta>
            {new Route.Meta()
                {
                    Shape = 2,
                    Attributes = new List<(string key, string value)>
                    {
                        ("ref","0")
                    }
                },
                new Route.Meta()
                {
                    Shape = 2,
                    Attributes = new List<(string key, string value)>
                    {
                        ("ref","1")
                    }
                }
            }
        };
        r.RemoveDuplicateShapeMeta(false); // Keep last
        Assert.Single(r.ShapeMeta);
       
        Assert.Equal("1",  r.ShapeMeta[0].Attributes.ToList()[0].value);
    }
}
