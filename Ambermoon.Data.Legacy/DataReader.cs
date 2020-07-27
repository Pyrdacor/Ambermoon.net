using System;
using System.Drawing;
using System.IO;
using System.Net.Security;
using System.Text;

namespace Ambermoon.Data.Legacy
{
    using word = UInt16;
    using dword = UInt32;

    internal class DataReader : IDataReader
    {
        private static readonly Encoding encoding;
        protected readonly byte[] _data;
        private int _position = 0;
        public int Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > Size)
                    throw new IndexOutOfRangeException("Data index out of range.");

                _position = value;
            }
        }
        public int Size => _data == null ? 0 : _data.Length;

        static DataReader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            encoding = Encoding.GetEncoding(437);
        }

        public DataReader(byte[] data, int offset, int length)
        {
            _data = new byte[length];
            Buffer.BlockCopy(data, offset, _data, 0, length);
        }

        public DataReader(byte[] data, int offset)
            : this(data, offset, data.Length - offset)
        {

        }

        public DataReader(byte[] data)
            : this(data, 0, data.Length)
        {

        }

        public DataReader(DataReader reader, int offset, int size)
            : this(reader._data, offset, size)
        {

        }

        public bool ReadBool()
        {
            CheckOutOfRange(1);
            return _data[Position++] != 0;
        }

        public byte ReadByte()
        {
            CheckOutOfRange(1);
            return _data[Position++];
        }

        public word ReadWord()
        {
            CheckOutOfRange(2);
            return (word)((_data[Position++] << 8) | _data[Position++]);
        }

        public dword ReadDword()
        {
            CheckOutOfRange(4);
            return (dword)((_data[Position++] << 24) | (_data[Position++] << 16) | (_data[Position++] << 8) | _data[Position++]);
        }

        public string ReadChar()
        {
            return ReadString(1);
        }

        public string ReadString()
        {
            CheckOutOfRange(1);
            int length = ReadByte();
            return ReadString(length);
        }

        public string ReadString(int length)
        {
            CheckOutOfRange(length);
            var str = encoding.GetString(_data, Position, length);
            Position += length;
            return str;
        }

        public byte PeekByte()
        {
            CheckOutOfRange(1);
            return _data[Position];
        }

        public word PeekWord()
        {
            CheckOutOfRange(2);
            return (word)((_data[Position] << 8) | _data[Position + 1]);
        }

        public dword PeekDword()
        {
            CheckOutOfRange(4);
            return (dword)((_data[Position] << 24) | (_data[Position + 1] << 16) | (_data[Position + 2] << 8) | _data[Position + 3]);
        }

        public byte[] ReadToEnd()
        {
            return ReadBytes(Size - Position);
        }

        public byte[] ReadBytes(int amount)
        {
            var data = new byte[amount];
            Buffer.BlockCopy(_data, Position, data, 0, data.Length);
            Position += amount;
            return data;
        }

        protected void CheckOutOfRange(int sizeToRead)
        {
            if (Position + sizeToRead > _data.Length)
                throw new IndexOutOfRangeException("Read beyond the data size.");
        }

        public long FindByteSequence(byte[] sequence, long offset)
        {
            if (_data == null)
                return -1;

            if (offset + sequence.Length > _data.Length)
                return -1;

            long lastIndex = _data.Length - sequence.Length;

            for (long i = offset; i <= lastIndex; ++i)
            {
                int j = 0;

                for (; j < sequence.Length; ++j)
                {
                    if (_data[i + j] != sequence[j])
                        break;
                }

                if (j == sequence.Length)
                    return i;
            }

            return -1;
        }

        public long FindString(string str, long offset)
        {
            return FindByteSequence(encoding.GetBytes(str), offset);
        }
    }
}
