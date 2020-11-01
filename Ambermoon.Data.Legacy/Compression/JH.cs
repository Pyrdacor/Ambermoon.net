using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class JH
    {
        /// <summary>
        /// This will de- or encrypt data with the JH encryption.
        /// </summary>
        /// <param name="data">Data to de-/encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <param name="offset">Offset inside the data where de-/encryption should start</param>
        /// <returns></returns>
        public static byte[] Crypt(byte[] data, ushort key, int offset = 0)
        {
            int numWords = (data.Length - offset + 1) >> 1;
            ushort d0 = key, d1;

            for (int i = 0; i < numWords; ++i)
            {
                int index = offset + i * 2;
                var value = (index == data.Length - 1) ? (ushort)((ushort)data[index] << 8) : ReadWord(data, index);
                value ^= d0;
                WriteWord(data, index, value);
                d1 = d0;
                d0 <<= 4;
                d0 = (ushort)((d0 + d1 + 87) & 0xffff);
            }

            return data;
        }

        public static byte[] Crypt(DataReader reader, ushort key, int offset = 0)
        {
            byte[] data = new byte[reader.Size - reader.Position];
            int numWords = (data.Length - offset + 1) >> 1;
            ushort d0 = key, d1;

            for (int i = 0; i < offset; ++i)
                data[i] = reader.ReadByte();

            for (int i = 0; i < numWords; ++i)
            {
                var value = (reader.Position == reader.Size - 1) ? (ushort)(reader.ReadByte() << 8) : reader.ReadWord();
                value ^= d0;
                WriteWord(data, offset + i * 2, value);
                d1 = d0;
                d0 <<= 4;
                d0 = (ushort)((d0 + d1 + 87) & 0xffff);
            }

            return data;
        }

        private static ushort ReadWord(byte[] data, int offset)
        {
            return (ushort)(((ushort)data[offset] << 8) | data[offset + 1]);
        }

        private static void WriteWord(byte[] data, int offset, ushort word)
        {
            data[offset] = (byte)(word >> 8);

            if (offset < data.Length - 1)
                data[offset + 1] = (byte)(word & 0xff);
        }
    }
}
