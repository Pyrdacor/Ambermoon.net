using System;

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
        string ReadString(int length);
        byte PeekByte();
        word PeekWord();
        dword PeekDword();
        int Position { get; set; }
        int Size { get; }
        byte[] ReadToEnd();
        byte[] ReadBytes(int amount);
    }
}
