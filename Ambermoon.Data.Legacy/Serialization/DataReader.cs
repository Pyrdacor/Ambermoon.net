using Ambermoon.Data.Serialization;
using SonicArranger;
using System;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.Legacy.Serialization
{
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;

    public class DataReader : IDataReader, ICustomReader
    {
        public static readonly Encoding Encoding;
        protected readonly byte[] data;
        private int position = 0;
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
        public int Size => data == null ? 0 : data.Length;

        static DataReader()
        {
            Encoding = new AmbermoonEncoding();
        }

        public DataReader(byte[] data, int offset, int length)
        {
            this.data = new byte[length];
            Buffer.BlockCopy(data, offset, this.data, 0, length);
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
            stream.Read(data, 0, data.Length);
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

        public bool ReadBool()
        {
            CheckOutOfRange(1);
            return data[Position++] != 0;
        }

        public byte ReadByte()
        {
            CheckOutOfRange(1);
            return data[Position++];
        }

        public word ReadWord()
        {
            CheckOutOfRange(2);
            return (word)((data[Position++] << 8) | data[Position++]);
        }

        public dword ReadDword()
        {
            CheckOutOfRange(4);
            return (((dword)data[Position++] << 24) | ((dword)data[Position++] << 16) | ((dword)data[Position++] << 8) | data[Position++]);
        }

        public qword ReadQword()
        {
            CheckOutOfRange(8);
            return (((qword)data[Position++] << 56) | ((qword)data[Position++] << 48) | ((qword)data[Position++] << 40) |
                ((qword)data[Position++] << 32) | ((qword)data[Position++] << 24) | ((qword)data[Position++] << 16) |
                ((qword)data[Position++] << 8) | data[Position++]);
        }

        public string ReadChar()
        {
            return ReadString(1);
        }

        public string ReadString()
        {
            return ReadString(Encoding);
        }

        public string ReadString(Encoding encoding)
        {
            CheckOutOfRange(1);
            int length = ReadByte();
            return ReadString(length, encoding);
        }

        public string ReadString(int length)
        {
            return ReadString(length, Encoding);
        }

        public string ReadString(int length, Encoding encoding)
        {
            CheckOutOfRange(length);
            var str = encoding.GetString(data, Position, length);
            str = str.Replace(encoding.GetString(new byte[] { 0xb4 }), "'");
            Position += length;
            return str;
        }

        public string ReadNullTerminatedString()
        {
            return ReadNullTerminatedString(Encoding);
        }

        public string ReadNullTerminatedString(Encoding encoding)
        {
            string result = "";
            byte[] buffer = new byte[1];

            while (Position < Size && (buffer[0] = ReadByte()) != 0)
            {
                if (buffer[0] == 0xb4)
                    result += "'";
                else
                    result += encoding.GetString(buffer);
            }

            return result;
        }

        public byte PeekByte()
        {
            CheckOutOfRange(1);
            return data[Position];
        }

        public word PeekWord()
        {
            CheckOutOfRange(2);
            return (word)((data[Position] << 8) | data[Position + 1]);
        }

        public dword PeekDword()
        {
            CheckOutOfRange(4);
            return (dword)((data[Position] << 24) | (data[Position + 1] << 16) | (data[Position + 2] << 8) | data[Position + 3]);
        }

        public byte[] ReadToEnd()
        {
            return ReadBytes(Size - Position);
        }

        public byte[] ReadBytes(int amount)
        {
            var data = new byte[amount];
            Buffer.BlockCopy(this.data, Position, data, 0, data.Length);
            Position += amount;
            return data;
        }

        protected void CheckOutOfRange(int sizeToRead)
        {
            if (Position + sizeToRead > data.Length)
                throw new System.IO.EndOfStreamException("Read beyond the data size.");
        }

        public long FindByteSequence(byte[] sequence, long offset)
        {
            if (data == null)
                return -1;

            if (offset + sequence.Length > data.Length)
                return -1;

            long lastIndex = data.Length - sequence.Length;

            for (long i = offset; i <= lastIndex; ++i)
            {
                int j = 0;

                for (; j < sequence.Length; ++j)
                {
                    if (data[i + j] != sequence[j])
                        break;
                }

                if (j == sequence.Length)
                    return i;
            }

            return -1;
        }

        public long FindString(string str, long offset)
        {
            return FindByteSequence(Encoding.GetBytes(str), offset);
        }

        public void AlignToWord()
        {
            if (Position % 2 == 1)
                ++Position;
        }

        public void AlignToDword()
        {
            if (Position % 4 != 0)
                Position += 4 - Position % 4;
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
