namespace Ambermoon
{
    public class PngCrc
    {
        uint[] table = null;
        uint lastCrc = 0;

        void EnsureTable()
        {
            if (table != null)
                return;

            table = new uint[256];
            uint c;
            uint k;

            for (uint n = 0; n < 256; ++n)
            {
                c = n;

                for (k = 0; k < 8; ++k)
                {
                    if ((c & 1) != 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c >>= 1;
                }

                table[n] = c;
            }
        }

        uint Update(uint crc, byte[] buffer, int length)
        {
            EnsureTable();
            uint c = crc;

            for (int n = 0; n < length; ++n)
            {
                c = table[(c ^ buffer[n]) & 0xff] ^ (c >> 8);
            }

            return c;
        }

        public uint Calculate(byte[] buffer)
        {
            return Calculate(buffer, buffer.Length);
        }

        public uint Calculate(byte[] buffer, int length)
        {
            return lastCrc = Update(lastCrc ^ 0xffffffff, buffer, length) ^ 0xffffffff;
        }

        public uint Calculate(uint crc, byte[] buffer)
        {
            return Calculate(crc, buffer, buffer.Length);
        }

        public uint Calculate(uint crc, byte[] buffer, int length)
        {
            lastCrc = crc;
            return Calculate(buffer, length);
        }
    }
}
