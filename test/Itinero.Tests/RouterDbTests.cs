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

using System.IO;
using System.Linq;
using Xunit;

namespace Itinero.Tests
{
    public class RouterDbTests
    {
        [Fact]
        public void RouterDbGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = routerDb.AddEdge(vertex1, vertex2);

            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }

        [Fact]
        public void RouterDb_ShouldStoreShape()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = network.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edges = network.AddEdge(vertex1, vertex2, shape: new [] { (4.795167446136475,
                51.26580191532799) });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edges.edge1);
            var shape = enumerator.Shape;
            Assert.NotNull(shape);
            var shapeList = shape.ToList();
            Assert.Single(shapeList);
            Assert.Equal(4.795167446136475, shapeList[0].longitude);
            Assert.Equal(51.26580191532799, shapeList[0].latitude);
        }

        [Fact]
        public void RouterDb_ShouldStoreAttributes()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = routerDb.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edges = routerDb.AddEdge(vertex1, vertex2, attributes: new [] { ("highway", "residential") });

            var attributes = routerDb.GetAttributes(edges.edge1);
            Assert.NotNull(attributes);
            Assert.Single(attributes);
            Assert.Equal("highway", attributes.First().key);
            Assert.Equal("residential", attributes.First().value);
        }

        [Fact]
        public void RouterDb_WriteReadShouldCopy()
        {
            var original = new RouterDb();
            var vertex1 = original.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = original.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edges = original.AddEdge(vertex1, vertex2, 
                shape: new [] { (4.795167446136475, 51.26580191532799) }, 
                attributes: new [] { ("highway", "residential") });

            using (var stream = new MemoryStream())
            {
                original.WriteTo(stream);

                stream.Seek(0, SeekOrigin.Begin);

                var routerDb = RouterDb.ReadFrom(stream);
                
                var enumerator = routerDb.GetEdgeEnumerator();
                enumerator.MoveToEdge(edges.edge1);
                var shape = enumerator.Shape;
                Assert.NotNull(shape);
                var shapeList = shape.ToList();
                Assert.Single(shapeList);
                Assert.Equal(4.795167446136475, shapeList[0].longitude);
                Assert.Equal(51.26580191532799, shapeList[0].latitude);
                var attributes = routerDb.GetAttributes(edges.edge1);
                Assert.NotNull(attributes);
                Assert.Single(attributes);
                Assert.Equal("highway", attributes.First().key);
                Assert.Equal("residential", attributes.First().value);
            }
        }
    }
}