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
 
 namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// An elevation handler.
    /// </summary>
    public static class ElevationHandler
    {
        /// <summary>
        /// Gets or sets the delegate to get elevation.
        /// </summary>
        public static GetElevationDelegate? GetElevation { get; set; }

        /// <summary>
        /// A delegate to get elevation.
        /// </summary>
        public delegate short? GetElevationDelegate(double longitude, double latitude);

        /// <summary>
        /// Add elevation to the given coordinate.
        /// </summary>
        public static short? Elevation(this (double longitude, double latitude) coordinate)
        {
            return GetElevation?.Invoke(coordinate.longitude, coordinate.latitude);
        }
    }
}