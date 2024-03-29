﻿/*
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
using Itinero.IO.Osm;
using Itinero.IO.Osm.Filters;

namespace Itinero.Tests.Functional.IO.Osm;

public static class RouterDbStreamTargetTests
{
    /// <summary>
    /// Loads a router db given the OSM file.
    /// </summary>
    /// <param name="osmPbfFile">An OSM-PBF file.</param>
    /// <returns>A loaded router db</returns>
    public static RouterDb LoadFrom(string osmPbfFile)
    {
        var routerDb = new RouterDb();

        using var stream = File.OpenRead(osmPbfFile);
        var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);
        var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
        progress.RegisterSource(source);

        routerDb.UseOsmData(progress);

        return routerDb;
    }
}
