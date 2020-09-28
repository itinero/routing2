using Reminiscence.Arrays;

namespace Itinero.Network.Storage
{
    internal static class ArrayBaseExtensions
    {
        public static ArrayBase<T> Clone<T>(this ArrayBase<T> arrayBase)
        {
            var memoryArray = new MemoryArray<T>(arrayBase.Length);
            memoryArray.CopyFrom(arrayBase);
            return memoryArray;
        }
    }
}