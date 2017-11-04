/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using Itinero.Algorithms.PriorityQueues;
using Itinero.Algorithms.Weights;
using Itinero.Data.Edges;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using Itinero.Algorithms;

namespace Itinero.Tiled.Algorithms
{
    /// <summary>
    /// An implementation of the dykstra routing algorithm.
    /// </summary>
    public class Dykstra<T> : AlgorithmBase
        where T : struct
    {
        private readonly GeometricGraph _graph;
        private readonly IEnumerable<TiledEdgePath<T>> _sources;
        private readonly Func<ulong, ulong> _getRestriction;
        private readonly T _sourceMax;
        private readonly bool _backward;
        private readonly WeightHandler<T> _weightHandler;

        /// <summary>
        /// Creates a new one-to-all dykstra algorithm instance.
        /// </summary>
        public Dykstra(GeometricGraph graph, Func<ulong, ulong> getRestriction, WeightHandler<T> weightHandler,
            IEnumerable<TiledEdgePath<T>> sources, T sourceMax, bool backward)
        {
            _graph = graph;
            _sources = sources;
            _sourceMax = sourceMax;
            _backward = backward;
            _getRestriction = getRestriction;
            _weightHandler = weightHandler;
        }
        
        private Dictionary<ulong, TiledEdgePath<T>> _visits;
        private TiledEdgePath<T> _current;
        private BinaryHeap<TiledEdgePath<T>> _heap;

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void DoRun()
        {
            // initialize stuff.
            this.Initialize();

            // start the search.
            while (this.Step()) { }
        }

        /// <summary>
        /// Initializes and resets.
        /// </summary>
        public void Initialize()
        {
            // algorithm always succeeds, it may be dealing with an empty network and there are no targets.
            this.HasSucceeded = true;

            // intialize dykstra data structures.
            _visits = new Dictionary<ulong, TiledEdgePath<T>>();
            _heap = new BinaryHeap<TiledEdgePath<T>>(1000);

            // queue all sources.
            foreach (var source in _sources)
            {
                if (_getRestriction != null)
                {
                    var restriction = _getRestriction(source.Vertex);
                    if (restriction != Constants.NO_VERTEX)
                    {
                        continue;
                    }
                }

                _heap.Push(source, _weightHandler.GetMetric(source.Weight));
            }
        }

        /// <summary>
        /// Executes one step in the search.
        /// </summary>
        public bool Step()
        {
            // while the visit list is not empty.
            _current = null;
            if (_heap.Count > 0)
            { // choose the next vertex.
                _current = _heap.Pop();
                while (_current != null && _visits.ContainsKey(_current.Vertex))
                { // keep dequeuing.
                    if (_heap.Count == 0)
                    { // nothing more to pop.
                        return false;
                    }

                    //if (this.Visit != null &&
                    //    this.Visit(_current))
                    //{ // edge was found and true was returned, this search should stop.
                    //    return false;
                    //}

                    _current = _heap.Pop();
                }
            }
            else
            { // no more visits possible.
                return false;
            }

            if (_current == null)
            { // route is not found, there are no vertices left
                return false;
            }

            // check for restrictions.
            ulong restriction = Constants.NO_VERTEX;
            if (_getRestriction != null)
            {
                restriction = _getRestriction(_current.Vertex);
            }
            if (restriction != Constants.NO_VERTEX)
            { // this vertex is restricted, step is a success but just move 
              // to the next one because this vertex's neighbours are not allowed.
                return true;
            }

            // we visit this one, set visit
            _visits[_current.Vertex] = _current;

            if (this.WasFound != null &&
                this.WasFound(_current.Vertex, _current.Weight))
            { // vertex was found and true was returned, this search should stop.
                return false;
            }

            //if (this.Visit != null &&
            //    this.Visit(_current))
            //{ // edge was found and true was returned, this search should stop.
            //    return false;
            //}

            // get neighbours and queue them.
            var edgeEnumerator = _graph.GetEdgeEnumerator(_current.Vertex);
            while (edgeEnumerator.MoveNext())
            {
                var edge = edgeEnumerator;
                var neighbour = edge.To;

                if (_current.From != null &&
                    _current.From.Vertex == neighbour)
                { // don't go back
                    continue;
                }

                //if (this.Visit == null)
                //{
                //    if (_visits.ContainsKey(neighbour))
                //    { // has already been choosen
                //        continue;
                //    }
                //}

                // get the speed from cache or calculate.
                float distance;
                ushort edgeProfile;
                EdgeDataSerializer.Deserialize(edge.Data0, out distance, out edgeProfile);
                var factor = Factor.NoFactor;
                var totalWeight = _weightHandler.Add(_current.Weight, edgeProfile, distance, out factor);

                // check the tags against the interpreter.
                if (factor.Value > 0 && (factor.Direction == 0 ||
                    (!_backward && (factor.Direction == 1) != edge.DataInverted) ||
                    (_backward && (factor.Direction == 1) == edge.DataInverted)))
                { // it's ok; the edge can be traversed by the given vehicle.
                    // calculate neighbors weight.
                    var edgeWeight = (distance * factor.Value);

                    if (_weightHandler.IsSmallerThan(totalWeight, _sourceMax))
                    { // update the visit list.
                        _heap.Push(new TiledEdgePath<T>(neighbour, totalWeight, edge.IdDirected, _current),
                            _weightHandler.GetMetric(totalWeight));
                    }
                    else
                    { // the maxium was reached.
                        this.MaxReached = true;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sets a visit on a vertex from an external source (like a transit-algorithm).
        /// </summary>
        /// <remarks>The algorithm will pick up these visits as if it was one it's own.</remarks>
        /// <returns>True if the visit was set successfully.</returns>
        public bool SetVisit(TiledEdgePath<T> visit)
        {
            if (!_visits.ContainsKey(visit.Vertex))
            {
                _heap.Push(visit, _weightHandler.GetMetric(visit.Weight));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given vertex was visited and sets the visit output parameters with the actual visit data.
        /// </summary>
        /// <returns></returns>
        public bool TryGetVisit(ulong vertex, out TiledEdgePath<T> visit)
        {
            return _visits.TryGetValue(vertex, out visit);
        }

        /// <summary>
        /// Gets the max reached flag.
        /// </summary>
        /// <remarks>True if the source-max value was reached.</remarks>
        public bool MaxReached { get; private set; }

        /// <summary>
        /// The was found delegate.
        /// </summary>
        public delegate bool WasFoundDelegate(ulong vertex, T weight);

        /// <summary>
        /// Gets or sets the wasfound function to be called when a new vertex is found.
        /// </summary>
        public WasFoundDelegate WasFound
        {
            get;
            set;
        }

        ///// <summary>
        ///// Gets or sets the visit function to be called when a new path is found.
        ///// </summary>
        //public VisitDelegate<T> Visit
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// Gets the backward flag.
        /// </summary>
        public bool Backward
        {
            get
            {
                return _backward;
            }
        }

        /// <summary>
        /// Gets the graph.
        /// </summary>
        public GeometricGraph Graph
        {
            get
            {
                return _graph;
            }
        }

        /// <summary>
        /// Gets the current.
        /// </summary>
        public TiledEdgePath<T> Current
        {
            get
            {
                return _current;
            }
        }
    }

    /// <summary>
    /// A default implementation of the generic dykstra algorithm.
    /// </summary>
    public sealed class Dykstra : Dykstra<float>
    {
        /// <summary>
        /// Creates a new one-to-all dykstra algorithm instance.
        /// </summary>
        public Dykstra(GeometricGraph graph, Func<ushort, Factor> getFactor, Func<ulong, ulong> getRestriction,
            IEnumerable<TiledEdgePath<float>> sources, float sourceMax, bool backward)
            : base(graph, getRestriction, new DefaultWeightHandler(getFactor), sources, sourceMax, backward)
        {

        }

        /// <summary>
        /// Creates a new one-to-all dykstra algorithm instance.
        /// </summary>
        public Dykstra(GeometricGraph graph, DefaultWeightHandler weightHandler, Func<ulong, ulong> getRestriction,
            IEnumerable<TiledEdgePath<float>> sources, float sourceMax, bool backward)
            : base(graph, getRestriction, weightHandler, sources, sourceMax, backward)
        {

        }
    }
}