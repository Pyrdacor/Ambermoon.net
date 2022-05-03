using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class DataStream
    {
        readonly List<byte> data;

        public int Position { get; set; }
        public int Size => data.Count;

        public byte[] ToArray() => data.ToArray();
        public byte[] ToArray(int offset, int length) => data.GetRange(offset, length).ToArray();

        public DataStream()
        {
            data = new List<byte>();
        }

        public DataStream(int capacity)
        {
            data = new List<byte>(capacity);
        }

        public DataStream(byte[] data)
        {
            this.data = new List<byte>(data);
        }

        public void Clear()
        {
            data.Clear();
        }

        public byte ReadByte()
        {
            return data[Position++];
        }

        public byte[] ReadBytes(int length)
        {
            var range = data.GetRange(Position, length);
            Position += length;
            return range.ToArray();
        }

        public void Read(byte[] data, int offset, int length)
        {
            var bytes = ReadBytes(length);
            Buffer.BlockCopy(data, offset, bytes, 0, length);
        }

        public void WriteByte(byte value)
        {
            if (Position == data.Count)
                AppendByte(value);
            else
                data[Position++] = value;
        }

        public void WriteBytes(byte[] bytes)
        {
            if (Position == data.Count)
                AppendBytes(bytes);
            else
            {
                foreach (var value in bytes)
                    WriteByte(value);
            }
        }

        public void AppendByte(byte value)
        {
            data.Add(value);
            Position = data.Count;
        }

        public void AppendBytes(byte[] bytes)
        {
            data.AddRange(bytes);
            Position = data.Count;
        }

        public void InsertByte(int index, byte value)
        {
            data.Insert(index, value);
            Position = index + 1;
        }

        public void InsertBytes(int index, byte[] bytes)
        {
            data.InsertRange(index, bytes);
            Position = index + bytes.Length;
        }

        public void RemoveRange(int index, int length)
        {
            data.RemoveRange(index, length);
        }

        public void Replace(int index, int length, byte[] data)
        {
            RemoveRange(index, length);
            InsertBytes(index, data);
        }

        public void Replace(int index, int length, IDataWriter writer)
        {
            Replace(index, length, writer.ToArray());
        }

        public IDisposableDataReader GetReader()
        {
            return new StreamedDataReader(data.ToArray());
        }

        public IDisposableDataReader GetReader(int offset, int length)
        {
            return new StreamedDataReader(data.GetRange(offset, length).ToArray());
        }
    }
}
