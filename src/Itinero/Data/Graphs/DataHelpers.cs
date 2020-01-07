namespace Itinero.Data.Graphs
{
    internal static class DataHelpers
    {
        /// <summary>
        /// Converts the given data into a nullable version.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The nullable version.</returns>
        public static uint? ToNullable(this uint data)
        {
            if (data == 0) return null;
            return data - 1;
        }

        /// <summary>
        /// Converts the given nullable data into a non-nullable version.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The non-nullable version.</returns>
        public static uint FromNullable(this uint? data)
        {
            if (!data.HasValue) return 0;
            return data.Value + 1;
        }
    }
}