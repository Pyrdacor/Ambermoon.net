using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.IO;
using System.Text;

namespace Ambermoon.Data.FileSystems
{
    internal class StreamedDataReader : IDisposableDataReader
    {
        readonly DataReader reader;
        readonly Stream stream;
        readonly bool leaveOpen;
        bool disposed = false;        

        public StreamedDataReader(Stream stream, bool leaveOpen)
        {
            if (!stream.CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            this.stream = stream;
            this.leaveOpen = leaveOpen;
            reader = new DataReader(stream);
        }

        public StreamedDataReader(Stream stream, int offset, int length, bool leaveOpen)
        {
            if (!stream.CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            this.stream = stream;
            this.leaveOpen = leaveOpen;
            reader = new DataReader(stream, offset, length);
        }

        public StreamedDataReader(byte[] data)
        {
            stream = null;
            leaveOpen = true;
            reader = new DataReader(data);
        }

        public StreamedDataReader(byte[] data, int offset, int length)
        {
            stream = null;
            leaveOpen = true;
            reader = new DataReader(data, offset, length);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (!leaveOpen)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }

        public int Position
        {
            get => reader.Position;
            set => reader.Position = value;
        }

        public int Size => reader.Size;

        public void AlignToDword()
        {
            reader.AlignToDword();
        }

        public void AlignToWord()
        {
            reader.AlignToWord();
        }

        public long FindByteSequence(byte[] sequence, long offset)
        {
            return reader.FindByteSequence(sequence, offset);
        }

        public long FindString(string str, long offset)
        {
            return reader.FindString(str, offset);
        }

        public byte PeekByte()
        {
            return reader.PeekByte();
        }

        public uint PeekDword()
        {
            return reader.PeekDword();
        }

        public ushort PeekWord()
        {
            return reader.PeekWord();
        }

        public bool ReadBool()
        {
            return reader.ReadBool();
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public byte[] ReadBytes(int amount)
        {
            return reader.ReadBytes(amount);
        }

        public string ReadChar()
        {
            return reader.ReadChar();
        }

        public uint ReadDword()
        {
            return reader.ReadDword();
        }

        public string ReadNullTerminatedString()
        {
            return reader.ReadNullTerminatedString();
        }

        public string ReadNullTerminatedString(Encoding encoding)
        {
            return reader.ReadNullTerminatedString(encoding);
        }

        public ulong ReadQword()
        {
            return reader.ReadQword();
        }

        public string ReadString()
        {
            return reader.ReadString();
        }

        public string ReadString(Encoding encoding)
        {
            return reader.ReadString(encoding);
        }

        public string ReadString(int length)
        {
            return reader.ReadString(length);
        }

        public string ReadString(int length, Encoding encoding)
        {
            return reader.ReadString(length, encoding);
        }

        public byte[] ReadToEnd()
        {
            return reader.ReadToEnd();
        }

        public ushort ReadWord()
        {
            return reader.ReadWord();
        }

        public byte[] ToArray()
        {
            return reader.ToArray();
        }
    }
}
