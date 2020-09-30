namespace Ambermoon
{
    public class Random
    {
        ushort[] state = new ushort[] { 0x1234, 0x5678 };

        public uint Next()
        {
            ushort result = state[1];

            // rotate state[0] bits 1 right and add 7
            state[1] ^= (ushort)((ushort)((ushort)((ushort)(state[0] >> 1) | (ushort)((ushort)(state[0] << 15) & 0xFFFF)) + 7) & 0xFFFF);
            state[0] = result;

            return result;
        }
    }
}
