using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Ambermoon.Data.Legacy.Serialization
{
    using word = UInt16;
    using dword = UInt32;

    public class DataWriter
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

        public DataWriter(DataWriter writer, int offset, int size)
            : this(writer.data.ToArray(), offset, size)
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

        public void Write(char value)
        {
            Write(value.ToString());
        }

        public void Write(string value)
        {
            if (value.Length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            Write((byte)value.Length);

            if (value.Length != 0)
                Write(Encoding.GetBytes(value));
        }

        public void Write(string value, int length)
        {
            if (length > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Strings must not exceed 255 characters.");

            if (length > value.Length)
                value += new string(' ', length - value.Length);
            else
                value = value.Substring(0, length);

            Write(value);
        }

        public void Write(byte[] bytes)
        {
            data.AddRange(bytes);
            Position += bytes.Length;
        }

        public void Replace(int offset, dword value)
        {
            if (offset + 4 > Size)
                throw new IndexOutOfRangeException("Index was outside the data writer size.");

            data[offset + 0] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        public void CopyTo(Stream stream)
        {
            stream.Write(data.ToArray(), 0, data.Count);
        }

        public byte[] ToArray() => data.ToArray();
    }
}
