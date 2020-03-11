namespace Itinero.Data.Graphs
{
    internal static class DataHelpers
    {
        public static uint? DecodeNullableData(this uint data)
        {
            if (data == 0) return null;
            return data - 1;
        }
        
        public static uint EncodeAsNullableData(this uint? data)
        {
            if (!data.HasValue) return 0;
            return data.Value + 1;
        }
        
        public static uint EncodeToNullableData(this uint data)
        {
            return data + 1;
        }
    }
}