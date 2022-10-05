using System.Linq;
using Itinero.IO.Osm.Streams;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Streams
{
    public class RelationTagsPreprocessorTests
    {
        [Fact]
        public void RelationTagsPreprocessor_NoRelevant_ShouldEnumerateAll()
        {
            var os = new OsmGeo[] {
                new Node() {
                    Id = 0
                },
                new Node() {
                    Id = 1
                },
                new Way() {
                    Id = 2,
                    Nodes = new []{ 0L, 1 }
                }
            };

            var completeStream = new RelationTagsPreprocessor(
                (c, o) => false);
            completeStream.RegisterSource(os);

            var result = completeStream.ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal(0, result[0]?.Id);
            Assert.Equal(1, result[1]?.Id);
            Assert.Equal(2, result[2]?.Id);
        }

        [Fact]
        public void RelationTagsPreprocessor_1Relevant_ShouldCallbackOnSecondPass()
        {
            var os = new OsmGeo[] {
                new Node() {
                    Id = 0
                },
                new Relation() {
                    Id = 2,
                    Members = new []{ new RelationMember() {
                        Id = 0,
                        Type = OsmGeoType.Node
                    }}
                }
            };

            var completeStream = new RelationTagsPreprocessor(
                (c, o) =>
                {
                    Assert.Equal(2, c.Id);
                    if (o != null) Assert.Equal(0, o.Id);
                    return true;
                });
            completeStream.RegisterSource(os);

            var result = completeStream.ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0]?.Id);
            Assert.Equal(2, result[1]?.Id);

            result = completeStream.ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0]?.Id);
            Assert.Equal(2, result[1]?.Id);
        }

        [Fact]
        public void RelationTagsPreprocessor_1Relevant_ReflectChangesOnSecondPass()
        {
            var os = new OsmGeo[] {
                new Node() {
                    Id = 0
                },
                new Relation() {
                    Id = 2,
                    Members = new []{ new RelationMember() {
                        Id = 0,
                        Type = OsmGeoType.Node
                    }}
                }
            };

            var completeStream = new RelationTagsPreprocessor(
                (c, o) =>
                {
                    if (o != null) o.Tags = new TagsCollection(new Tag("id", c.Id.ToInvariantString()));
                    return true;
                });
            completeStream.RegisterSource(os);

            var result = completeStream.ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0]?.Id);
            Assert.Equal(2, result[1]?.Id);

            result = completeStream.ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0]?.Id);
            Assert.NotNull(result[0].Tags);
            Assert.Equal(new Tag[] { new("id", "2") }, result[0].Tags);
            Assert.Equal(2, result[1]?.Id);
        }
    }
}
