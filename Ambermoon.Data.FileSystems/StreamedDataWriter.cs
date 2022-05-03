using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.IO;
using System.Text;

namespace Ambermoon.Data.FileSystems
{
    internal class StreamedDataWriter : IDisposableDataWriter
    {
        readonly DataWriter writer = new DataWriter();
        readonly Stream stream;
        readonly long start;
        readonly bool leaveOpen;
        readonly Action disposeHandler;
        bool disposed = false;        

        public StreamedDataWriter(Stream stream, bool leaveOpen)
        {
            if (!stream.CanWrite)
                throw new NotSupportedException("Stream does not support writing.");

            this.stream = stream;
            start = stream.Position;
            this.leaveOpen = leaveOpen;
        }

        public StreamedDataWriter(Stream stream, bool leaveOpen, Action disposeHandler)
        {
            if (!stream.CanWrite)
                throw new NotSupportedException("Stream does not support writing.");

            this.stream = stream;
            start = stream.Position;
            this.leaveOpen = leaveOpen;
            this.disposeHandler = disposeHandler;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                disposeHandler?.Invoke();

                if (!leaveOpen)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }

        public int Position => writer.Position;

        public int Size => writer.Size;

        public void CopyTo(Stream stream)
        {
            writer.CopyTo(stream);
        }

        void UpdateStream(int relativePosition, int length)
        {
            if (disposed)
                throw new InvalidOperationException("Stream is already disposed.");

            long pos = stream.Position;
            stream.Position = start + relativePosition;

            for (int i = 0; i < length; ++i)
                stream.WriteByte(writer[relativePosition + i]);

            stream.Position = pos;
        }

        void AddToStream(int offset, int length)
        {
            if (disposed)
                throw new InvalidOperationException("Stream is already disposed.");

            for (int i = 0; i < length; ++i)
                stream.WriteByte(writer[offset + i]);
        }

        public void Replace(int offset, bool value)
        {
            if (offset == Size)
                Write(value);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, value);
            UpdateStream(offset, 1);
        }

        public void Replace(int offset, byte value)
        {
            if (offset == Size)
                Write(value);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, value);
            UpdateStream(offset, sizeof(byte));
        }

        public void Replace(int offset, ushort value)
        {
            if (offset == Size)
                Write(value);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, value);
            UpdateStream(offset, sizeof(ushort));
        }

        public void Replace(int offset, uint value)
        {
            if (offset == Size)
                Write(value);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, value);
            UpdateStream(offset, sizeof(uint));
        }

        public void Replace(int offset, ulong value)
        {
            if (offset == Size)
                Write(value);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, value);
            UpdateStream(offset, sizeof(ulong));
        }

        public void Replace(int offset, byte[] data)
        {
            if (offset == Size)
                Write(data);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, data);
            UpdateStream(offset, data.Length);
        }

        public void Replace(int offset, byte[] data, int dataOffset)
        {
            if (offset == Size)
                Write(data);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, data, dataOffset);
            UpdateStream(offset, data.Length - dataOffset);
        }

        public void Replace(int offset, byte[] data, int dataOffset, int length)
        {
            if (offset == Size)
                Write(data);

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            writer.Replace(offset, data, dataOffset, length);
            UpdateStream(offset, length);
        }

        public byte[] ToArray() => writer.ToArray();

        public void Write(bool value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(byte value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(ushort value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(uint value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(ulong value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(char value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(string value)
        {
            int offset = writer.Size;
            writer.Write(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(string value, int length, char fillChar = ' ')
        {
            int offset = writer.Size;
            writer.Write(value, length, fillChar);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(string value, Encoding encoding)
        {
            int offset = writer.Size;
            writer.Write(value, encoding);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(string value, Encoding encoding, int length, char fillChar = ' ')
        {
            int offset = writer.Size;
            writer.Write(value, encoding, length, fillChar);
            AddToStream(offset, writer.Size - offset);
        }

        public void Write(byte[] bytes)
        {
            int offset = writer.Size;
            writer.Write(bytes);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteNullTerminated(string value)
        {
            int offset = writer.Size;
            writer.WriteNullTerminated(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteNullTerminated(string value, Encoding encoding)
        {
            int offset = writer.Size;
            writer.WriteNullTerminated(value, encoding);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteWithoutLength(string value)
        {
            int offset = writer.Size;
            writer.WriteWithoutLength(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteWithoutLength(string value, Encoding encoding)
        {
            int offset = writer.Size;
            writer.WriteWithoutLength(value, encoding);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteEnumAsByte<T>(T value) where T : struct, System.Enum, IConvertible
        {
            int offset = writer.Size;
            writer.WriteEnumAsByte(value);
            AddToStream(offset, writer.Size - offset);
        }

        public void WriteEnumAsWord<T>(T value) where T : struct, System.Enum, IConvertible
        {
            int offset = writer.Size;
            writer.WriteEnumAsWord(value);
            AddToStream(offset, writer.Size - offset);
        }
    }
}
