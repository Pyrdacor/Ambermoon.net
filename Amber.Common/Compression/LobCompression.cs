using Amber.Serialization;

namespace Amber.Compression
{
    public static class LobCompression
    {
        public enum LobType : byte
        {
            TakeBest = 0x00, // Use original or extended or advanced LOB based on compression size
            TakeBestForText = 0x01, // Use original or text LOB based on compression size
            Ambermoon = 0x06,
            LZRS = 0x10,
            Extended = 0x11,
            Text = 0x12        
        }

        public static byte[] Compress(byte[] data, LobType lobType = LobType.Ambermoon)
        {
            return lobType switch
            {
                LobType.LZRS => ExtendedLob.CompressData(data),
                LobType.Extended => AdvancedLob.CompressData(data),
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
                LobType.LZRS => ExtendedLob.Decompress(reader, decodedSize),
                LobType.Extended => AdvancedLob.Decompress(reader, decodedSize),
                LobType.Text => TextLob.Decompress(reader, decodedSize),
                _ => Lob.Decompress(reader, decodedSize),
            };
        }
    }
}
