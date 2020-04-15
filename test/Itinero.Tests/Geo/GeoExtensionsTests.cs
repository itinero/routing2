using Itinero.Geo;
using Xunit;

namespace Itinero.Tests.Geo
{
    public class GeoExtensionsTests
    {
        [Fact]
        public void GeoExtensions_Box_Center()
        {
            var line = ((4.79615062671703, 51.2691260384684),(4.79962761847527, 51.2701600445663));
            var center = line.Center();
            
            Assert.Equal((4.79615062671703 + 4.79962761847527) / 2, center.longitude);
            Assert.Equal((51.2691260384684 + 51.2701600445663) / 2, center.latitude);
        }

        [Fact]
        public void GeoExtensions_OffsetWithDistanceX_Offset10_ShouldOffset10()
        {
            (double longitude, double latitude) start = (4.79615062671703, 51.2691260384684);
            var offset = start.OffsetWithDistanceX(10);
            
            Assert.Equal(10, start.DistanceEstimateInMeter(offset), 1);
            Assert.True(offset.longitude > start.longitude);
        }

        [Fact]
        public void GeoExtensions_OffsetWithDistanceX_OffsetNeg10_ShouldOffsetNeg10()
        {
            (double longitude, double latitude) start = (4.79615062671703, 51.2691260384684);
            var offset = start.OffsetWithDistanceX(-10);
            
            Assert.Equal(10, start.DistanceEstimateInMeter(offset), 1);
            Assert.True(offset.longitude < start.longitude);
        }
        
        [Fact]
        public void GeoExtensions_OffsetWithDistanceY_Offset10_ShouldOffset10()
        {
            (double longitude, double latitude) start = (4.79615062671703, 51.2691260384684);
            var offset = start.OffsetWithDistanceY(10);
            
            Assert.Equal(10, start.DistanceEstimateInMeter(offset), 1);
            Assert.True(offset.latitude > start.latitude);
        }

        [Fact]
        public void GeoExtensions_OffsetWithDistanceY_OffsetNeg10_ShouldOffsetNeg10()
        {
            (double longitude, double latitude) start = (4.79615062671703, 51.2691260384684);
            var offset = start.OffsetWithDistanceY(-10);
            
            Assert.Equal(10, start.DistanceEstimateInMeter(offset), 1);
            Assert.True(offset.latitude < start.latitude);
        }
        
        [Fact]
        public void GeoExtensions_Intersect_2IntersectingLines_ShouldReturnIntersection()
        {
            (double longitude, double latitude) start1 = (3.7312209606170654,51.05363599762037);
            (double longitude, double latitude) start2 = (3.7311029434204100,51.05344379131031);
            
            var line1 = (start1, start1.OffsetWithDistanceX(10));
            var line2 = (start2, start2.OffsetWithDistanceX(10));
            var intersection = line1.Intersect(line2);
            
            Assert.Null(intersection);
        }
        
        [Fact]
        public void GeoExtensions_Intersect_2NonIntersectingLines_ShouldReturnNull()
        {
            var line1 = ((3.7312209606170654,51.05363599762037),(3.7314248085021973,51.053366234152264));
            var line2 = ((3.7311029434204100,51.05344379131031),(3.7315267324447630,51.053588789126906));
            var intersection = line1.Intersect(line2);
            
            Assert.NotNull(intersection);
            Assert.Equal(3.73131212057064,intersection.Value.longitude,6);
            Assert.Equal(51.0535153604835,intersection.Value.latitude,6);
        }
        
        [Fact]
        public void GeoExtensions_Intersect_2IntersectingLines_SegmentsNotAfter_ShouldReturnNull()
        {
            var line1 = ((3.7312209606170654,51.05363599762037),(3.7314248085021973,51.053366234152264));
            var line2 = ((3.7307462096214294,51.053764134717206),(3.731285333633423,51.05395296768736));
            var intersection = line1.Intersect(line2);
            
            Assert.Null(intersection);
        }
        
        [Fact]
        public void GeoExtensions_Intersect_2IntersectingLines_SegmentsNotBefore_ShouldReturnNull()
        {
            var line1 = ((3.7312209606170654,51.05363599762037),(3.7314248085021973,51.053366234152264));
            var line2 = ((3.7313148379325862,51.052995306817746),(3.7319532036781307,51.05318077085639));
            var intersection = line1.Intersect(line2);
            
            Assert.Null(intersection);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line1_Point1_ShouldReturnClosest()
        {
            var line = ((4.79615062671703, 51.2691260384684),(4.79962761847527, 51.2701600445663));
            (double longitude, double latitude) original = (4.798139333724975,51.26923890547876);
            (double longitude, double latitude) expected = (4.79784277124878, 51.2696292573099);
            var project = line.ProjectOn(original);
            
            Assert.NotNull(project);
            Assert.Equal(expected.longitude, project.Value.longitude, 6);
            Assert.Equal(expected.latitude, project.Value.latitude, 6);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line1Reversed_Point1_ShouldReturnClosest()
        {
            var line = ((4.79962761847527, 51.2701600445663), (4.79615062671703, 51.2691260384684));
            (double longitude, double latitude) original = (4.798139333724975,51.26923890547876);
            (double longitude, double latitude) expected = (4.79784277124878, 51.2696292573099);
            var project = line.ProjectOn(original);
            
            Assert.NotNull(project);
            Assert.Equal(expected.longitude, project.Value.longitude, 6);
            Assert.Equal(expected.latitude, project.Value.latitude, 6);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line2_Point1_ShouldReturnClosest()
        {
            var line = ((4.79615062671703, 51.270160044566),(4.79962761847527, 51.26912603846843));
            (double longitude, double latitude) original = (4.798139333724975,51.26923890547876);
            (double longitude, double latitude) expected = (4.79834367480668, 51.26950786439);
            var project = line.ProjectOn(original);
            
            Assert.NotNull(project);
            Assert.Equal(expected.longitude, project.Value.longitude, 6);
            Assert.Equal(expected.latitude, project.Value.latitude, 6);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line2Reversed_Point1_ShouldReturnClosest()
        {
            var line = ((4.79962761847527, 51.26912603846843), (4.79615062671703, 51.270160044566));
            (double longitude, double latitude) original = (4.798139333724975,51.26923890547876);
            (double longitude, double latitude) expected = (4.79834367480668, 51.26950786439);
            var project = line.ProjectOn(original);
            
            Assert.NotNull(project);
            Assert.Equal(expected.longitude, project.Value.longitude, 6);
            Assert.Equal(expected.latitude, project.Value.latitude, 6);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line1_Point2OutsideSegment_ShouldReturnNull()
        {
            var line = ((4.79615062671703, 51.2691260384684),(4.79962761847527, 51.2701600445663));
            (double longitude, double latitude) original = (4.801046848297119,
                51.27006790661612);
            
            var project = line.ProjectOn(original);
            
            Assert.Null(project);
        }
        
        [Fact]
        public void GeoExtensions_ProjectOn_Line1_Center_ShouldReturnCenter()
        {
            var line = ((4.79615062671703, 51.2691260384684),(4.79962761847527, 51.2701600445663));
            var center = line.Center();
            var project = line.ProjectOn(center);
            
            Assert.NotNull(project);
            Assert.Equal(project.Value.longitude, center.longitude, 6);
            Assert.Equal(project.Value.latitude, center.latitude, 6);
        }
    }
}