namespace SonicArranger
{
    public struct WaveTable
    {
        public byte[] Data { get; private set; }

        internal WaveTable(ICustomReader reader) : this()
        {
            Data = reader.ReadBytes(128);
        }
    }
}
