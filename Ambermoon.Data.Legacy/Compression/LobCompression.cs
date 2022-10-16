using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class LobCompression
    {
        public enum LobType : byte
        {
            TakeBest = 0x00, // Use original or extended or advanced LOB based on compression size
            TakeBestForText = 0x01, // Use original or text LOB based on compression size
            Ambermoon = 0x06,
            Extended = 0x10,
            Advanced = 0x11,
            Text = 0x12        
        }

        public static byte[] Compress(byte[] data, LobType lobType = LobType.Ambermoon)
        {
            return lobType switch
            {
                LobType.Extended => ExtendedLob.CompressData(data),
                LobType.Advanced => AdvancedLob.CompressData(data),
                LobType.Text => TextLob.CompressData(data),
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
                LobType.Extended => ExtendedLob.Decompress(reader, decodedSize),
                LobType.Advanced => AdvancedLob.Decompress(reader, decodedSize),
                LobType.Text => TextLob.Decompress(reader, decodedSize),
                _ => Lob.Decompress(reader, decodedSize),
            };
        }
    }
}
