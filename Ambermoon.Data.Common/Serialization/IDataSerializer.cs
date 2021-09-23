using System;
using System.Text;

namespace Ambermoon.Data.Serialization
{
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;

    public enum WriteOperation
    {
        Insert,
        Override,
        Append
    }

    public interface IDataSerializer : IDataReader, IDataWriter
    {
        WriteOperation WriteOperation { get; set; }
        void Append(bool value);
        void Append(byte value);
        void Append(word value);
        void Append(dword value);
        void Append(qword value);
        void Append(char value);
        void Append(string value);
        void Append(string value, int length, char fillChar = ' ');
        void Append(string value, Encoding encoding);
        void Append(string value, Encoding encoding, int length, char fillChar = ' ');
        void Append(byte[] bytes);
        void AppendEnumAsByte<T>(T value) where T : struct, System.Enum, IConvertible;
        void AppendEnumAsWord<T>(T value) where T : struct, System.Enum, IConvertible;
        void AppendNullTerminated(string value);
        void AppendNullTerminated(string value, Encoding encoding);
        void AppendWithoutLength(string value);
        void AppendWithoutLength(string value, Encoding encoding);
    }
}
