using System.Text;

namespace Amber
{
#pragma warning disable CS8981
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;
#pragma warning restore CS8981

    public interface IDataReader
    {
        bool ReadBool();
        byte ReadByte();
        word ReadWord();
        dword ReadDword();
        qword ReadQword();
        string ReadChar();
        string ReadString();
        string ReadString(Encoding encoding);
        string ReadString(int length);
        string ReadString(int length, Encoding encoding);
        string ReadNullTerminatedString();
        string ReadNullTerminatedString(Encoding encoding);
        byte PeekByte();
        word PeekWord();
        dword PeekDword();
        int Position { get; set; }
        int Size { get; }
        byte[] ReadToEnd();
        byte[] ReadBytes(int amount);
        long FindByteSequence(byte[] sequence, long offset);
        long FindString(string str, long offset);
        void AlignToWord();
        void AlignToDword();
        byte[] ToArray();
    }
}
