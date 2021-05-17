namespace SonicArranger
{
    public struct Sample
    {
        /// <summary>
        /// Raw 8-bit signed data.
        /// </summary>
        public byte[] Data { get; private set; }

        internal Sample(ICustomReader reader, int size) : this()
        {
            Data = reader.ReadBytes(size);
        }
    }
}
