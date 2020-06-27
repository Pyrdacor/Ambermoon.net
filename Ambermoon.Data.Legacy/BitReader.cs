using System;

namespace Ambermoon.Data.Legacy
{
    public class BitReader
    {
        private readonly byte[] _buffer;
        private int _byteIndex = 0;
        private int _bitIndex = 0;

        public BitReader(byte[] data)
        {
            _buffer = data;
        }

        public int ReadBits(int amount)
        {
            if (amount < 0)
                throw new IndexOutOfRangeException("Reading a negative amount of bits is not supported.");

            if (amount > 8)
                throw new IndexOutOfRangeException("Legacy bit reader only supports reading of up to 8 bits.");

            if (amount == 0)
                return 0;

            if (_bitIndex + amount <= 8)
            {
                int shift = 8 - amount - 1;
                byte mask = (byte)((1 << amount) - 1);
                int result = (_buffer[_byteIndex] >> shift) & mask;

                _bitIndex += amount;

                if (_bitIndex > 7)
                {
                    _bitIndex %= 8;
                    ++_byteIndex;
                }

                return result;
            }
            else
            {
                int upperAmount = 8 - _bitIndex;
                int lowerAmount = amount - upperAmount;
                int upper = ReadBits(upperAmount);
                int lower = ReadBits(lowerAmount);
                return (upper << (8 - lowerAmount)) | lower;
            }
        }
    }
}
