using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Streams.Processors
{
    public class AddLengthTagPreprocessor : PreprocessorStream
    {
        /**
         * The stream is not a complete stream - we thus have to keep track of the nodes where we want to calculate a length for.
         * _keptNodes contains the nodes we encountered already, whereas _neededNodes is the wishlist of what we want
         */
        private readonly Dictionary<long, Node> _keptNodes = new();

        private readonly HashSet<long> _neededNodes = new();


        private readonly Predicate<Way> _onlyOnTheseFeatures;


        public AddLengthTagPreprocessor(OsmStreamSource actualSource, Predicate<Way> onlyOnTheseFeatures = null) : base(
            actualSource, 2)
        {
            _onlyOnTheseFeatures = onlyOnTheseFeatures ?? (_ => true);
        }

        private protected override void PreprocessFeature(OsmGeo feature)
        {
            if (feature is Node n) {
                if (n.Id != null && _neededNodes.Contains(n.Id.Value)) {
                    _keptNodes.Add(n.Id.Value, n);
                }

                return;
            }


            if (feature is not Way w) {
                return;
            }

            if (!_onlyOnTheseFeatures(w)) {
                return;
            }
                
                
            var l = CalculateLength(this.GetOrMarkCoordinates(w));
            if (l >= 0) {
                w.Tags.Add("_length", l.ToString());
            }


        }

        /**
         * Gets the needed coordinates from _keptNodes or adds them to the wishlist _neededNodes
         */
        private List<Node> GetOrMarkCoordinates(Way w)
        {
            var nodes = new List<Node>();
            foreach (var nodeId in w.Nodes) {
                if (_keptNodes.ContainsKey(nodeId)) {
                    nodes.Add(_keptNodes[nodeId]);
                }
                else {
                    _neededNodes.Add(nodeId);
                }
            }

            return nodes;
        }

        private static int CalculateLength(List<Node> nodes)
        {
            double distance = 0;
            for (var index = 1; index < nodes.Count; index++) {
                var start = nodes[index - 1];
                var end = nodes[index];
                var d = start.DistanceTo(end);
                if (d == null) {
                    continue;
                }

                distance += d.Value;
            }

            return (int) distance;
        }
    }
}