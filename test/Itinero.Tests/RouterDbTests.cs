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

using System.Linq;
using Itinero.Data;
using Itinero.Data.Attributes;
using Itinero.LocalGeo;
using NUnit.Framework;

namespace Itinero.Tests
{
    public class RouterDbTests
    {
        [Test]
        public void RouterDbGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = routerDb.AddEdge(vertex1, vertex2);

            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(vertex1, enumerator.From);
            Assert.AreEqual(vertex2, enumerator.To);
            Assert.AreEqual(true, enumerator.Forward);
            Assert.AreEqual(0, enumerator.CopyDataTo(new byte[10]));
            Assert.AreEqual(0, enumerator.Data.Length);
        }

        [Test]
        public void RouterDb_ShouldStoreShape()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = network.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edgeId = network.AddEdge(vertex1, vertex2, shape: new [] { new Coordinate(4.795167446136475,
                51.26580191532799), });

            var shape = network.GetShape(edgeId);
            Assert.IsNotNull(shape);
            var shapeList = shape.ToList();
            Assert.AreEqual(1, shapeList.Count);
            Assert.AreEqual(4.795167446136475, shapeList[0].Longitude);
            Assert.AreEqual(51.26580191532799, shapeList[0].Latitude);
        }

        [Test]
        public void RouterDb_ShouldStoreAttributes()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = routerDb.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edgeId = routerDb.AddEdge(vertex1, vertex2, attributes: new [] { new Attribute("highway", "residential"), });

            var attributes = routerDb.GetAttributes(edgeId);
            Assert.IsNotNull(attributes);
            Assert.AreEqual(1, attributes.Count);
            Assert.AreEqual("highway", attributes.First().Key);
            Assert.AreEqual("residential", attributes.First().Value);
        }
    }
}