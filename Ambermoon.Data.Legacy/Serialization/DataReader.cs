using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ambermoon.Data.Serialization;
using SonicArranger;

namespace Ambermoon.Data.Legacy.Serialization
{
#pragma warning disable CS8981
    using dword = UInt32;
    using qword = UInt64;
    using word = UInt16;
#pragma warning restore CS8981

    public class DataReader : IDataReader, ICustomReader
    {
        public static readonly Encoding Encoding;
        private readonly byte[] data;
        private int position;

        public int Position
        {
            get => position;
            set
            {
                if (value < 0 || value > Size)
                    throw new IndexOutOfRangeException("Data index out of range.");

                position = value;
            }
        }

        private byte[] Data { init => data = value; }

        public int Size => data.Length;

        private ReadOnlySpan<byte> Span => data;

        public byte this[int index] => data[index];

        static DataReader()
        {
            Encoding = new AmbermoonEncoding();
        }

        private DataReader()
        {
            position = 0;
        }

        public DataReader(byte[] data, int offset, int length)
        {
            this.data = new byte[length];
            Buffer.BlockCopy(data, offset, this.data, 0, length);
            position = 0;
        }

        public DataReader(byte[] data, int offset)
            : this(data, offset, data.Length - offset)
        {

        }

        public DataReader(byte[] data)
            : this(data, 0, data.Length)
        {

        }

        public DataReader(IDataReader reader, int offset, int size)
            : this(reader.ToArray(), offset, size)
        {

        }

        public DataReader(System.IO.Stream stream)
        {
            long pos = stream.CanSeek ? stream.Position : -1;
            data = new byte[stream.Length];
            stream.ReadExactly(data, 0, data.Length);
            if (pos != -1)
                stream.Position = pos;
        }

        public DataReader(System.IO.Stream stream, int offset, int size)
        {
            if (!stream.CanSeek && offset != stream.Position)
                throw new NotSupportedException("Stream does not support seeking.");

            long pos = stream.CanSeek ? stream.Position : -1;
            if (offset != stream.Position)
                stream.Position = offset;
            data = new byte[size];
            stream.Read(data, 0, size);
            if (pos != -1)
                stream.Position = pos;
        }

        public static DataReader FromData(byte[] data)
        {
            return new DataReader
            {
                Data = data
            };
        }

        public void AlignToDword()
        {
            position = (position + 3) & ~3;
        }

        public void AlignToWord()
        {
            position = (position + 1) & ~1;
        }

        public long FindByteSequence(byte[] sequence, long offset)
        {
            if (offset + sequence.Length > data.Length)
                return -1;

            int index = Span.Slice((int)offset).IndexOf(sequence);

            return index < 0 ? -1 : offset + index;
        }

        public long FindString(string str, long offset)
        {
            return FindByteSequence(DataReader.Encoding.GetBytes(str), offset);
        }

        public byte PeekByte()
        {
            return data[position];
        }

        public word PeekWord()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(Span[position..]);
        }

        public dword PeekDword()
        {
            return BinaryPrimitives.ReadUInt32BigEndian(Span[position..]);
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public byte ReadByte()
        {
            return data[position++];
        }

        public byte[] ReadBytes(int amount)
        {
            var bytes = Span.Slice(position, amount).ToArray();
            position += amount;
            return bytes;
        }

        public string ReadChar() => ReadString(1);

        public dword ReadDword()
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(Span[position..]);
            position += 4;
            return value;
        }

        public word ReadWord()
        {
            ushort value = BinaryPrimitives.ReadUInt16BigEndian(Span[position..]);
            position += 2;
            return value;
        }

        public qword ReadQword()
        {
            ulong value = BinaryPrimitives.ReadUInt64BigEndian(Span[position..]);
            position += 8;
            return value;
        }

        public string ReadNullTerminatedString() => ReadNullTerminatedString(Encoding);

        public string ReadNullTerminatedString(Encoding encoding)
        {
            List<byte> buffer = [];
            byte b;
            bool needMoreBytes = false;

            while (Position < Size && ((b = ReadByte()) != 0 || needMoreBytes))
            {
                buffer.Add(b);

                // When parsing multi-byte encodings there might be characters which
                // end with a 00-byte. As this is also used for termination we have
                // to check for character ending if the next byte is 00.
                if (!encoding.IsSingleByte && Position < Size && PeekByte() == 0)
                {
                    try
                    {
                        encoding.GetString(buffer.ToArray());
                    }
                    catch (ArgumentException)
                    {
                        needMoreBytes = true;
                    }
                }
            }

            try
            {
                return encoding.GetString(buffer.ToArray());
            }
            catch (ArgumentException)
            {
                return encoding.GetString(buffer.Take(buffer.Count - 1).ToArray()) + "?";
            }
        }

        public string ReadString() => ReadString(Encoding);

        public string ReadString(Encoding encoding)
        {
            int length = ReadByte();
            return ReadString(length, encoding);
        }

        public string ReadString(int length) => ReadString(length, Encoding);

        public string ReadString(int length, Encoding encoding)
        {
            if (length == 0)
                return string.Empty;

            var str = encoding.GetString(data, position, length);
            str = str.Replace(encoding.GetString([0xb4]), "'");
            position += length;
            return str;
        }

        public byte[] ReadToEnd()
        {
            var result = Span[position..].ToArray();
            position = Size;
            return result;
        }

        public byte[] ToArray() => data;


        // SonicArranger.ICustomReader implementation
        public char[] ReadChars(int amount) => Encoding.ASCII.GetChars(ReadBytes(amount));
        public short ReadBEInt16() => unchecked((short)ReadWord());
        public ushort ReadBEUInt16() => ReadWord();
        public int ReadBEInt32() => unchecked((int)ReadDword());
        public uint ReadBEUInt32() => ReadDword();
    }
}
