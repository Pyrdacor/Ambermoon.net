using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions
{
    internal class NullCompression : ICompression
    {
        public ushort Identifier => 0x0000;

        public IDataReader Decompress(IDataReader dataReader)
        {
            return dataReader;
        }

        public IDataWriter Compress(IDataWriter dataWriter)
        {
            return dataWriter;
        }
    }
}
