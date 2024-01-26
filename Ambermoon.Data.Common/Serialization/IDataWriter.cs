using System;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Serialization
{
#pragma warning disable CS8981
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;
#pragma warning restore CS8981

    public interface IDataWriter
    {
        int Position { get; }
        int Size { get; }
        void Write(bool value);
        void Write(byte value);
        void Write(word value);
        void Write(dword value);
        void Write(qword value);
        void Write(char value);
        void Write(string value);
        void Write(string value, int length, char fillChar = ' ');
        void Write(string value, Encoding encoding);
        void Write(string value, Encoding encoding, int length, char fillChar = ' ');
        void Write(byte[] bytes);
        void Replace(int offset, bool value);
        void Replace(int offset, byte value);
        void Replace(int offset, word value);
        void Replace(int offset, dword value);
        void Replace(int offset, qword value);
        void Replace(int offset, byte[] data);
        void Replace(int offset, byte[] data, int dataOffset);
        void Replace(int offset, byte[] data, int dataOffset, int length);
        void CopyTo(Stream stream);
        byte[] ToArray();
        byte[] GetBytes(int offset, int length);
        void WriteEnumAsByte<T>(T value) where T : struct, Enum, IConvertible;
        void WriteEnumAsWord<T>(T value) where T : struct, Enum, IConvertible;
        void WriteNullTerminated(string value);
        void WriteNullTerminated(string value, Encoding encoding);
        void WriteWithoutLength(string value);
        void WriteWithoutLength(string value, Encoding encoding);
    }
}
