namespace SonicArranger
{
    public interface ICustomReader
    {
        char[] ReadChars(int amount);
        byte ReadByte();
        byte[] ReadBytes(int amount);
        short ReadBEInt16();
        ushort ReadBEUInt16();
        int ReadBEInt32();
        uint ReadBEUInt32();
        int Position { get; set; }
        int Size { get; }
    }
}
