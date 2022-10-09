using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class LobCompression
    {
        public enum LobType : byte
        {
            Ambermoon = 0x06,
            Text = 0xfe,
            Extended = 0xff
        }

        public static byte[] Compress(byte[] data, LobType lobType = LobType.Ambermoon)
        {
            return lobType switch
            {
                LobType.Text => TextLob.CompressData(data),
                LobType.Extended => ExtendedLob.CompressData(data),
                _ => Lob.CompressData(data),
            };
        }

        public static DataReader Decompress(byte[] data, uint decodedSize, LobType lobType = LobType.Ambermoon)
        {
            return Decompress(new DataReader(data), decodedSize, lobType);
        }

        public static DataReader Decompress(IDataReader reader, uint decodedSize, LobType lobType = LobType.Ambermoon)
        {
            return lobType switch
            {
                LobType.Text => TextLob.Decompress(reader, decodedSize),
                LobType.Extended => ExtendedLob.Decompress(reader, decodedSize),
                _ => Lob.Decompress(reader, decodedSize),
            };
        }
    }
}
