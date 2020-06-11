using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.IO.Osm.Restrictions;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions
{
    public class RestrictionParserTests
    {
        [Fact]
        public void RestrictionParser_GetVertexSequence_ViaNode()
        {
            var relation = new Relation()
            {
                Id = 1,
                Members = new []
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(1, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way), 
                },
                Tags = new TagsCollection()
                {
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_right_turn")
                }
            };
            
            var data = new Dictionary<OsmGeoKey, OsmGeo>()
            {
                {new OsmGeoKey(OsmGeoType.Node, 1), new Node { Id = 1 }},
                {new OsmGeoKey(OsmGeoType.Way, 1), new Way { Id = 1, Nodes = new long[] {2, 1} }},
                {new OsmGeoKey(OsmGeoType.Way, 2), new Way { Id = 2, Nodes = new long[] {1, 3} }}
            };

            var result = relation.GetVertexSequence(n => new VertexId(901448234, (uint)n), key =>
            {
                if (!data.TryGetValue(key, out var value)) return null;

                return value;
            });
            Assert.False(result.IsError);
            var sequence = result.Value.ToList();
            Assert.Equal(3, sequence.Count);
            Assert.Equal(2U, sequence[0].LocalId);
            Assert.Equal(1U, sequence[1].LocalId);
            Assert.Equal(3U, sequence[2].LocalId);
        }
        
        [Fact]
        public void RestrictionParser_GetVertexSequence_1ViaWay()
        {
            var relation = new Relation()
            {
                Id = 1,
                Members = new []
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Way),
                    new RelationMember(3, "to", OsmGeoType.Way), 
                },
                Tags = new TagsCollection()
                {
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_right_turn")
                }
            };
            
            var data = new Dictionary<OsmGeoKey, OsmGeo>()
            {
                {new OsmGeoKey(OsmGeoType.Way, 1), new Way { Id = 1, Nodes = new long[] {1, 2} }},
                {new OsmGeoKey(OsmGeoType.Way, 2), new Way { Id = 2, Nodes = new long[] {2, 3} }},
                {new OsmGeoKey(OsmGeoType.Way, 3), new Way { Id = 3, Nodes = new long[] {3, 4} }}
            };

            var result = relation.GetVertexSequence(n => new VertexId(901448234, (uint)n), key =>
            {
                if (!data.TryGetValue(key, out var value)) return null;

                return value;
            });
            Assert.False(result.IsError);
            var sequence = result.Value.ToList();
            Assert.Equal(4, sequence.Count);
            Assert.Equal(1U, sequence[0].LocalId);
            Assert.Equal(2U, sequence[1].LocalId);
            Assert.Equal(3U, sequence[2].LocalId);
            Assert.Equal(4U, sequence[3].LocalId);
        }
        
        [Fact]
        public void RestrictionParser_GetVertexSequence_2ViaWay()
        {
            var relation = new Relation()
            {
                Id = 1,
                Members = new []
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Way),
                    new RelationMember(3, "via", OsmGeoType.Way), 
                    new RelationMember(4, "to", OsmGeoType.Way), 
                },
                Tags = new TagsCollection()
                {
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_right_turn")
                }
            };
            
            var data = new Dictionary<OsmGeoKey, OsmGeo>()
            {
                {new OsmGeoKey(OsmGeoType.Way, 1), new Way { Id = 1, Nodes = new long[] {1, 2} }},
                {new OsmGeoKey(OsmGeoType.Way, 2), new Way { Id = 2, Nodes = new long[] {2, 3} }},
                {new OsmGeoKey(OsmGeoType.Way, 3), new Way { Id = 3, Nodes = new long[] {3, 4} }},
                {new OsmGeoKey(OsmGeoType.Way, 4), new Way { Id = 3, Nodes = new long[] {4, 5} }}
            };

            var result = relation.GetVertexSequence(n => new VertexId(901448234, (uint)n), key =>
            {
                if (!data.TryGetValue(key, out var value)) return null;

                return value;
            });
            Assert.False(result.IsError);
            var sequence = result.Value.ToList();
            Assert.Equal(5, sequence.Count);
            Assert.Equal(1U, sequence[0].LocalId);
            Assert.Equal(2U, sequence[1].LocalId);
            Assert.Equal(3U, sequence[2].LocalId);
            Assert.Equal(4U, sequence[3].LocalId);
            Assert.Equal(5U, sequence[4].LocalId);
        }
    }
}