namespace Itinero
{
    public struct SnapPoint
    {
        public SnapPoint(uint edgeId, ushort offset)
        {
            this.EdgeId = edgeId;
            this.Offset = offset;
        }
        
        public uint EdgeId { get; private set; }
        
        public ushort Offset { get; private set; }

        public override string ToString()
        {
            return $"{this.EdgeId} @ {this.Offset}";
        }
    }
}