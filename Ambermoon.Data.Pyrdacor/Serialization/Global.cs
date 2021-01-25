namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public enum Endianness
    {
        Little,
        Big,
    }

    static class Global
    {
        // Note: This is big endian.
        public const uint BaseContainerHeader = 0xB0550000;
        public const byte DataVersion = 0;
    }
}
