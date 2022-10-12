using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class LobCompression
    {
        public enum LobType : byte
        {
            TakeBest = 0x00, // Use original or extended LOB based on compression size
            TakeBestForText = 0x01, // Use original or text LOB based on compression size
            TakeBestForTexture = 0x02, // Use original or texture LOB based on compression size
            Ambermoon = 0x06,
            Texture = 0xfd,
            Text = 0xfe,
            Extended = 0xff
        }

        public static byte[] Compress(byte[] data, LobType lobType = LobType.Ambermoon)
        {
            return lobType switch
            {
                LobType.Texture => ExtendedLob.CompressData(data, true),
                LobType.Text => TextLob.CompressData(data),
                LobType.Extended => ExtendedLob.CompressData(data, false),
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
                LobType.Texture => ExtendedLob.Decompress(reader, decodedSize, true),
                LobType.Text => TextLob.Decompress(reader, decodedSize),
                LobType.Extended => ExtendedLob.Decompress(reader, decodedSize, false),
                _ => Lob.Decompress(reader, decodedSize),
            };
        }
    }
}
