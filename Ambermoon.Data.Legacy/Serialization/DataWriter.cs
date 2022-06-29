using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;

    public class DataWriter : IDataWriter
    {
        public static readonly Encoding Encoding = DataReader.Encoding;
        protected readonly List<byte> data = new List<byte>();
        public int Position { get; private set; } = 0;
        public int Size => data.Count;

        public DataWriter()
        {

        }

        public DataWriter(byte[] data, int offset, int length)
        {
            var tempData = new byte[length];
            Buffer.BlockCopy(data, offset, tempData, 0, length);
            this.data.AddRange(tempData);
        }

        public DataWriter(byte[] data, int offset)
            : this(data, offset, data.Length - offset)
        {

        }

        public DataWriter(byte[] data)
            : this(data, 0, data.Length)
        {

        }

        public DataWriter(IDataWriter writer, int offset, int size)
            : this(writer.ToArray(), offset, size)
        {

        }

        public void Write(bool value)
        {
            data.Add((byte)(value ? 1 : 0));
            ++Position;
        }

        public void Write(byte value)
        {
            data.Add(value);
            ++Position;
        }

        public void Write(word value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
            Position += 2;
        }

        public void Write(dword value)
        {
            data.Add((byte)(value >> 24));
            data.Add((byte)(value >> 16));
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
            Position += 4;
        }

        public void Write(qword value)
        {
            data.Add((byte)(value >> 56));
            data.Add((byte)(value >> 48));
            data.Add((byte)(value >> 40));
            data.Add((byte)(value >> 32));
            data.Add((byte)(value >> 24));
            data.Add((byte)(value >> 16));
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
            Position += 8;
        }

        public void Write(char value)
        {
            Write(value.ToString());
        }

        public void Write(string value)
        {
            Write(value, Encoding);
        }

        public void Write(string value, int length, char fillChar = ' ')
        {
            Write(value, Encoding, length, fillChar);
        }

        public void Write(string value, Encoding encoding)
        {
            var bytes = encoding.GetBytes(value);

            if (bytes.Length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            Write((byte)bytes.Length);

            if (bytes.Length != 0)
                Write(bytes);
        }

        public void Write(string value, Encoding encoding, int length, char fillChar = ' ')
        {
            if (length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            if (length > value.Length)
                value += new string(fillChar, length - value.Length);
            else
                value = value.Substring(0, length);

            Write(value, encoding);
        }

        public void WriteNullTerminated(string value)
        {
            WriteNullTerminated(value, Encoding);
        }

        public void WriteNullTerminated(string value, Encoding encoding)
        {
            Write(encoding.GetBytes(value + "\0"));
        }

        public void WriteWithoutLength(string value)
        {
            WriteWithoutLength(value, Encoding);
        }

        public void WriteWithoutLength(string value, Encoding encoding)
        {
            Write(encoding.GetBytes(value));
        }

        public void Write(byte[] bytes)
        {
            data.AddRange(bytes);
            Position += bytes.Length;
        }

        public void Replace(int offset, bool value)
        {
            Replace(offset, (byte)(value ? 1 : 0));
        }

        public void Replace(int offset, byte value)
        {
            if (offset < 0 || offset + 1 > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            data[offset] = value;
        }

        public void Replace(int offset, word value)
        {
            if (offset < 0 || offset + 2 > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            data[offset + 0] = (byte)(value >> 8);
            data[offset + 1] = (byte)value;
        }

        public void Replace(int offset, dword value)
        {
            if (offset < 0 || offset + 4 > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            data[offset + 0] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        public void Replace(int offset, qword value)
        {
            if (offset < 0 || offset + 8 > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            data[offset + 0] = (byte)(value >> 56);
            data[offset + 1] = (byte)(value >> 48);
            data[offset + 2] = (byte)(value >> 40);
            data[offset + 3] = (byte)(value >> 32);
            data[offset + 4] = (byte)(value >> 24);
            data[offset + 5] = (byte)(value >> 16);
            data[offset + 6] = (byte)(value >> 8);
            data[offset + 7] = (byte)value;
        }

        public void Replace(int offset, byte[] data)
        {
            Replace(offset, data, 0);
        }

        public void Replace(int offset, byte[] data, int dataOffset)
        {
            Replace(offset, data, dataOffset, data.Length - dataOffset);
        }

        public void Replace(int offset, byte[] data, int dataOffset, int length)
        {
            if (dataOffset < 0 || dataOffset + length > data.Length)
                throw new IndexOutOfRangeException("Index was outside the given data array.");

            if (offset < 0 || offset + data.Length > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            for (int i = 0; i < length; ++i)
                this.data[offset + i] = data[dataOffset + i];
        }

        public void CopyTo(Stream stream)
        {
            stream.Write(data.ToArray(), 0, data.Count);
        }

        public byte[] ToArray() => data.ToArray();

        public byte[] GetBytes(int offset, int length)
        {
            return data.GetRange(offset, length).ToArray();
        }

        public byte this[int index] => data[index];

        public void WriteEnumAsByte<T>(T value) where T : struct, System.Enum, IConvertible => Write(value.ToByte(null));

        public void WriteEnumAsWord<T>(T value) where T : struct, System.Enum, IConvertible => Write(value.ToUInt16(null));
    }
}
