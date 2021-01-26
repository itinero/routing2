using System.Collections.Generic;
using System.Linq;
using Itinero.IO.Osm.Restrictions;
using Itinero.Network;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions
{
    public class RestrictionParserTests
    {
        [Fact]
        public void RestrictionParser_GetEdgeSequence_OneViaNode_ForwardForward_ShouldForwardForwardSequence()
        {
            var relation = new Relation {
                Id = 1,
                Members = new[] {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(1, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection {
                    new("type", "restriction"),
                    new("restriction", "no_right_turn")
                }
            };

            var data = new Dictionary<OsmGeoKey, OsmGeo> {
                {new OsmGeoKey(OsmGeoType.Node, 1), new Node {Id = 1}},
                {new OsmGeoKey(OsmGeoType.Way, 1), new Way {Id = 1, Nodes = new long[] {2, 1}}},
                {new OsmGeoKey(OsmGeoType.Way, 2), new Way {Id = 2, Nodes = new long[] {1, 3}}}
            };

            IEnumerable<(VertexId from, VertexId to, EdgeId id)> GetEdges(long wayId)
            {
                switch (wayId) {
                    case 1:
                        yield return (new VertexId(0, 2), new VertexId(0, 1), new EdgeId(0, 1));
                        break;
                    case 2:
                        yield return (new VertexId(0, 1), new VertexId(0, 1), new EdgeId(0, 2));
                        break;
                }
            }

            var result = relation.GetEdgeSequence(n => new VertexId(0, (uint) n),
                GetEdges);

            Assert.False(result.IsError);
            var sequence = result.Value.ToList();
            Assert.Equal(2, sequence.Count);
            Assert.True(sequence[0].forward);
            Assert.Equal(1U, sequence[0].edge.LocalId);
            Assert.True(sequence[1].forward);
            Assert.Equal(2U, sequence[1].edge.LocalId);
        }

        [Fact]
        public void RestrictionParser_GetEdgeSequence_OneViaWay_NotSupported()
        {
            var relation = new Relation {
                Id = 1,
                Members = new[] {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Way),
                    new RelationMember(3, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection {
                    new("type", "restriction"),
                    new("restriction", "no_right_turn")
                }
            };

            var result = relation.GetEdgeSequence(n => new VertexId(0, (uint) n),
                w => Enumerable.Empty<(VertexId from, VertexId to, EdgeId id)>());
            Assert.True(result.IsError);
        }
    }
}