using System;
using System.Text;

namespace Ambermoon.Data
{
    using word = UInt16;
    using dword = UInt32;

    public interface IDataReader
    {
        bool ReadBool();
        byte ReadByte();
        word ReadWord();
        dword ReadDword();
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
    }
}
