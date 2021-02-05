using System;
using System.IO;
using Itinero.IO.Streams.Processors;
using OsmSharp;
using OsmSharp.Streams;
using Xunit;

namespace Itinero.Tests.Processors
{
    public class ProcessorsTest
    {
        /// <summary>
        ///     A relation going over 'elfjulistraat' below
        /// </summary>
        private static readonly string cyclehighwayRelation =
            "<relation id=\"11484939\" visible=\"true\" version=\"5\" changeset=\"97191789\" timestamp=\"2021-01-08T21:45:19Z\" user=\"JosV\" uid=\"170722\">\n" +
            "<member type=\"way\" ref=\"28717919\" role=\"forward\"/>" +
            "<tag k=\"cycle_network\" v=\"cycle_highway\"/>\n" +
            "<tag k=\"name\" v=\"F6 Fietssnelweg Brugge - Aalter\"/>\n" +
            "<tag k=\"network\" v=\"ncn\"/>\n" +
            "<tag k=\"operator\" v=\"Provincie West-Vlaanderen;Provincie Oost-Vlaanderen\"/>\n" +
            "<tag k=\"ref\" v=\"F6\"/>\n" +
            "<tag k=\"route\" v=\"bicycle\"/>\n" +
            "<tag k=\"type\" v=\"route\"/>\n" +
            "<tag k=\"website\" v=\"https://fietssnelwegen.be/f6\"/>" +
            "</relation>";

        private static readonly string pieterColpaertsteeg =
            "<way id=\"28717919\" visible=\"true\" version=\"20\" changeset=\"86608041\" timestamp=\"2020-06-13T19:39:27Z\" user=\"Pieter Vander Vennet\" uid=\"3818858\">\n" +
            "<nd ref=\"315739244\"/>\n" +
            "<nd ref=\"315739310\"/>\n" +
            "<nd ref=\"4982676734\"/>\n" +
            "<nd ref=\"315739311\"/>\n" +
            "<nd ref=\"1976830098\"/>\n" +
            "<nd ref=\"5190253533\"/>\n" +
            "<nd ref=\"315739312\"/>\n" +
            "<tag k=\"highway\" v=\"living_street\"/>\n" +
            "<tag k=\"maxspeed\" v=\"30\"/>\n" +
            "<tag k=\"name\" v=\"Pieter Colpaertsteeg\"/>\n" +
            "<tag k=\"name:etymology:wikidata\" v=\"Q76797345\"/>\n" +
            "<tag k=\"surface\" v=\"paving_stones\"/>\n" +
            "</way>\n" +
            "<node id=\"315739312\" visible=\"true\" version=\"7\" changeset=\"9017732\" timestamp=\"2011-08-14T16:06:46Z\" user=\"Rastatter\" uid=\"121389\" lat=\"51.2172230\" lon=\"3.2190922\"/>\n" +
            "<node id=\"5190253533\" visible=\"true\" version=\"1\" changeset=\"53255973\" timestamp=\"2017-10-26T07:47:41Z\" user=\"Pieter Vander Vennet\" uid=\"3818858\" lat=\"51.2172098\" lon=\"3.2191132\"/>" +
            "<node id=\"1976830098\" visible=\"true\" version=\"2\" changeset=\"16684201\" timestamp=\"2013-06-24T14:18:36Z\" user=\"StijnW\" uid=\"1533583\" lat=\"51.2171997\" lon=\"3.2191293\"/>\n" +
            "<node id=\"315739311\" visible=\"true\" version=\"4\" changeset=\"50432147\" timestamp=\"2017-07-20T12:33:04Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2170509\" lon=\"3.2193952\"/>\n" +
            "<node id=\"4982676734\" visible=\"true\" version=\"1\" changeset=\"50432147\" timestamp=\"2017-07-20T12:32:36Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2169794\" lon=\"3.2194712\"/>\n" +
            "<node id=\"315739310\" visible=\"true\" version=\"4\" changeset=\"50432147\" timestamp=\"2017-07-20T12:33:04Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2169068\" lon=\"3.2195170\"/>\n" +
            "<node id=\"315739244\" visible=\"true\" version=\"6\" changeset=\"9748264\" timestamp=\"2011-11-05T17:37:27Z\" user=\"zors1843\" uid=\"233248\" lat=\"51.2154859\" lon=\"3.2203253\"/>\n";


        private static readonly string elfJulistraat =
            "<way id=\"28717919\" visible=\"true\" version=\"20\" changeset=\"86608041\" timestamp=\"2020-06-13T19:39:27Z\" user=\"Pieter Vander Vennet\" uid=\"3818858\">\n<nd ref=\"315739244\"/>\n<nd ref=\"315739310\"/>\n<nd ref=\"4982676734\"/>\n<nd ref=\"315739311\"/>\n<nd ref=\"1976830098\"/>\n<nd ref=\"5190253533\"/>\n<nd ref=\"315739312\"/>\n" +
            "<tag k=\"cycleway\" v=\"opposite\"/>\n<tag k=\"highway\" v=\"residential\"/>\n<tag k=\"lit\" v=\"yes\"/>\n<tag k=\"maxspeed\" v=\"30\"/>\n<tag k=\"name\" v=\"Elf-Julistraat\"/>\n<tag k=\"name:etymology:wikidata\" v=\"Q277589\"/>\n<tag k=\"oneway\" v=\"yes\"/>\n<tag k=\"oneway:bicycle\" v=\"no\"/>\n<tag k=\"parking:lane:both\" v=\"parallel\"/>\n<tag k=\"sidewalk\" v=\"both\"/>\n<tag k=\"sidewalk:surface\" v=\"paving_stones\"/>\n<tag k=\"surface\" v=\"sett\"/>\n<tag k=\"width:carriageway\" v=\"6.7\"/>\n<tag k=\"wikidata\" v=\"Q2674933\"/>\n<tag k=\"wikipedia\" v=\"nl:Elf-Julistraat\"/>\n</way>\n" +
            "<node id=\"315739312\" visible=\"true\" version=\"7\" changeset=\"9017732\" timestamp=\"2011-08-14T16:06:46Z\" user=\"Rastatter\" uid=\"121389\" lat=\"51.2172230\" lon=\"3.2190922\"/>\n" +
            "<node id=\"5190253533\" visible=\"true\" version=\"1\" changeset=\"53255973\" timestamp=\"2017-10-26T07:47:41Z\" user=\"Pieter Vander Vennet\" uid=\"3818858\" lat=\"51.2172098\" lon=\"3.2191132\"/>" +
            "<node id=\"1976830098\" visible=\"true\" version=\"2\" changeset=\"16684201\" timestamp=\"2013-06-24T14:18:36Z\" user=\"StijnW\" uid=\"1533583\" lat=\"51.2171997\" lon=\"3.2191293\"/>\n" +
            "<node id=\"315739311\" visible=\"true\" version=\"4\" changeset=\"50432147\" timestamp=\"2017-07-20T12:33:04Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2170509\" lon=\"3.2193952\"/>\n" +
            "<node id=\"4982676734\" visible=\"true\" version=\"1\" changeset=\"50432147\" timestamp=\"2017-07-20T12:32:36Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2169794\" lon=\"3.2194712\"/>\n" +
            "<node id=\"315739310\" visible=\"true\" version=\"4\" changeset=\"50432147\" timestamp=\"2017-07-20T12:33:04Z\" user=\"catweazle67\" uid=\"1976209\" lat=\"51.2169068\" lon=\"3.2195170\"/>\n" +
            "<node id=\"315739244\" visible=\"true\" version=\"6\" changeset=\"9748264\" timestamp=\"2011-11-05T17:37:27Z\" user=\"zors1843\" uid=\"233248\" lat=\"51.2154859\" lon=\"3.2203253\"/>\n";

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private OsmStreamSource GenerateStreamSource(string contents)
        {
            contents =
                "<osm version=\"0.6\" generator=\"CGImap 0.8.3 (3596456 spike-06.openstreetmap.org)\" copyright=\"OpenStreetMap and contributors\" attribution=\"http://www.openstreetmap.org/copyright\" license=\"http://opendatacommons.org/licenses/odbl/1-0/\">\n" +
                contents +
                "</osm>";

            return new XmlOsmStreamSource(GenerateStreamFromString(contents));
        }

        private static void IterateTillDone(PreprocessorStream stream, Action<OsmGeo> test)
        {
            for (var i = 0; i < stream.NeededPasses; i++) {
                while (stream.MoveNext()) {
                    if (i + 1 == stream.NeededPasses) {
                        test(stream.Current());
                    }
                }

                stream.Reset();
            }
        }

        [Fact]
        public void LengthProcessor_SimpleWay_WayHasLength()
        {
            var addLength = new AddLengthTagPreprocessor(this.GenerateStreamSource(elfJulistraat));
            IterateTillDone(addLength, geo => {
                if (geo.Type != OsmGeoType.Way) {
                    return;
                }

                var value = geo.Tags.GetValue("_length");
                Assert.NotNull(value);
                var l = int.Parse(value);
                Assert.Equal(214, l);
            });
        }

        [Fact]
        public void RelationPreprocessor_CycleRelation_TagsAreAdded()
        {
            var input = this.GenerateStreamSource(cyclehighwayRelation + elfJulistraat);
            var relationPrep = new AddRelationTagsPreprocessor(input,
                _ => true, "_relation:cyclehighway", (_, role) => role);
            IterateTillDone(relationPrep, geo => {
                if (!(geo is Way w)) {
                    return;
                }

                Assert.Equal("forward", w.Tags.GetValue("_relation:cyclehighway"));
            });
        }

        [Fact]
        public void RelationPreprocessor_NoMatch_NoAction()
        {
            var input = this.GenerateStreamSource(cyclehighwayRelation + elfJulistraat);
            var relationPrep = new AddRelationTagsPreprocessor(input,
                _ => false, "_relation:cyclehighway", (_, role) => role);
            IterateTillDone(relationPrep, geo => {
                if (!(geo is Way w)) {
                    return;
                }

                var value = w.Tags.GetValue("_relation:cyclehighway");
                Assert.Empty(value);
            });
        }

        [Fact]
        public void RelationPreprocessor_NestedStreams_BothActionsAreDone()
        {
            var input = this.GenerateStreamSource(cyclehighwayRelation + elfJulistraat);
            var relationPrep = new AddRelationTagsPreprocessor(input,
                _ => true, "_relation:cyclehighway", (_, role) => role);
            var lengthAdd = new AddLengthTagPreprocessor(relationPrep);
            IterateTillDone(lengthAdd, geo => {
                if (!(geo is Way w)) {
                    return;
                }

                var value = w.Tags.GetValue("_relation:cyclehighway");
                Assert.Equal("forward", value);
                Assert.Equal("214", w.Tags.GetValue("_length"));
            });
        }

        [Fact]
        public void AddExternalDataTagger_WikidataEntries_GenderIsIncluded()
        {

            var input = this.GenerateStreamSource(pieterColpaertsteeg);
            var tagger = new ExternalDataTagger(input, "../../../Processors/Genders.csv", "name:etymology:wikidata", "_gender");
            IterateTillDone(tagger, geo => {
                    if (!(geo is Way)) {
                        return;
                    }
                    Assert.Equal("M", geo.Tags.GetValue("_gender"));
                }
                );

        }
    }
}