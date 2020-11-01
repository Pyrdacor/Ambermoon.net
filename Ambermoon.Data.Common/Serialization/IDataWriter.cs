using System;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Serialization
{
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;

    public interface IDataWriter
    {
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
        void Replace(int offset, dword value);
        void CopyTo(Stream stream);
        byte[] ToArray();
        void WriteEnumAsByte<T>(T value) where T : struct, System.Enum, IConvertible;
        void WriteEnumAsWord<T>(T value) where T : struct, System.Enum, IConvertible;
    }
}
