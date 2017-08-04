using Reminiscence.Arrays;

namespace routing2
{
    /// <summary>
    /// A memory mapping profile for vertex tile deserialization.
    /// </summary>
    public class VertexTileProfile
    {
        /// <summary>
        /// Gets or sets the vertex profile.
        /// </summary>
        /// <returns></returns>
        public ArrayProfile VertexProfile { get; set; }

        /// <summary>
        /// Gets or sets the edge profile.
        /// </summary>
        /// <returns></returns>
        public ArrayProfile EdgeProfile { get; set; } 

        /// <summary>
        /// Gets or sets the coordinates profile.
        /// </summary>
        /// <returns></returns>
        public ArrayProfile CoordinatesProfile { get; set; }
    }
}