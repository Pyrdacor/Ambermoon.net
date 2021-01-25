using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Pyrdacor.Serialization.Compression
{
    static class RLE
    {
        public unsafe static byte[] Compress(byte[] data)
        {
            List<byte> result = new List<byte>(data.Length);
            byte rleByte = data[0];
            ushort rleWord = BitConverter.ToUInt16(data);
            uint rleDword = BitConverter.ToUInt32(data);
            int rleByteCount = -1;
            int rleWordCount = -1;
            int rleDwordCount = -1;
            int literalCount = 0;
            byte? threeByteRle = null;

            fixed (byte* ptr = data)
            {
                byte* end = ptr + data.Length;
                byte* bPtr = ptr;
                ushort* wPtr = (ushort*)ptr;
                uint* dPtr = (uint*)ptr;
                int byteNo = 0;
                byte* literalOffset = ptr;

                void Reset()
                {
                    rleByteCount = -1;
                    rleWordCount = -1;
                    rleDwordCount = -1;
                    literalCount = 0;
                    threeByteRle = null;
                    Increment(true);
                    rleByte = *bPtr;
                    rleWord = *wPtr;
                    rleDword = *dPtr;
                    literalOffset = bPtr;
                }

                void Increment(bool resetByteNo)
                {
                    ++bPtr;
                    if (resetByteNo)
                    {
                        byteNo = 0;
                        wPtr = (ushort*)bPtr;
                        dPtr = (uint*)bPtr;
                    }
                    else
                    {
                        ++byteNo;
                        if (byteNo % 2 == 0)
                        {
                            ++wPtr;
                            if (byteNo % 4 == 0)
                                ++dPtr;
                        }
                    }
                }

                void WriteLiterals(int excludeNumberOfBytes = 0)
                {
                    literalCount -= excludeNumberOfBytes;

                    if (literalCount <= 0)
                        throw new AmbermoonException(ExceptionScope.Application, "Logic error. Literal count should never by 0 here.");

                    int numFullBlocks = (literalCount - 1) / 127;
                    byte* endLiterals;

                    for (int i = 0; i < numFullBlocks; ++i)
                    {
                        result.Add(127);
                        endLiterals = literalOffset + 127;
                        while (literalOffset != endLiterals)
                            result.Add(*literalOffset++);
                    }

                    int remainingCount = literalCount % 128;
                    result.Add((byte)remainingCount);
                    endLiterals = literalOffset + remainingCount;
                    while (literalOffset != endLiterals)
                        result.Add(*literalOffset++);
                }

                while (bPtr != end)
                {
                    if (*bPtr == rleByte)
                    {
                        if (++rleByteCount == 103)
                        {
                            if (++literalCount > 103)
                                WriteLiterals(103);
                            result.Add(0);
                            result.Add(rleByte);
                            Reset();
                            continue;
                        }
                    }

                    // Give dwords prio over words so check it first
                    if (byteNo % 4 == 0 && *dPtr == rleDword)
                    {
                        if (++rleDwordCount == 15)
                        {
                            if (++literalCount > 15 * 4)
                                WriteLiterals(15 * 4);
                            result.Add(unchecked((byte)(-115)));
                            result.Add((byte)(rleDword >> 24));
                            result.Add((byte)(rleDword >> 16));
                            result.Add((byte)(rleDword >> 8));
                            result.Add((byte)rleDword);
                            Reset();
                            continue;
                        }
                    }

                    if (byteNo % 2 == 0 && *wPtr == rleWord)
                    {
                        if (++rleWordCount == 15)
                        {
                            if (++literalCount > 15 * 2)
                                WriteLiterals(15 * 2);
                            result.Add(unchecked((byte)(-101)));
                            result.Add((byte)(rleWord >> 8));
                            result.Add((byte)rleWord);
                            Reset();
                            continue;
                        }
                    }

                    if (*bPtr != rleByte && literalCount == 0 && rleDwordCount <= 1 && rleWordCount <= 1 && rleByteCount == 3)
                    {
                        threeByteRle = rleByte;
                        rleByte = *bPtr;
                        rleByteCount = 1;
                        literalCount = 1;
                    }
                    else if (literalCount != 0 || *bPtr != rleByte)
                        ++literalCount;

                    if (rleByteCount < 4)
                    {
                        if (rleWordCount <= 1 && byteNo % 4 == 0 && *dPtr != rleDword)
                        {
                            if (rleDwordCount > 1)
                            {
                                result.Add(unchecked((byte)(rleDwordCount - 130)));
                                result.Add((byte)(rleDword >> 24));
                                result.Add((byte)(rleDword >> 16));
                                result.Add((byte)(rleDword >> 8));
                                result.Add((byte)rleDword);
                                Reset();
                                continue;
                            }
                            else if (threeByteRle != null)
                            {
                                result.Add(unchecked((byte)-100));
                                result.Add(threeByteRle.Value);
                                Reset();
                                continue;
                            }
                            else
                            {
                                WriteLiterals();
                                Reset();
                                continue;
                            }
                        }
                        else if (rleWordCount > 1 && byteNo % 2 == 0 && *wPtr != rleWord)
                        {
                            result.Add(unchecked((byte)(rleWordCount - 116)));
                            result.Add((byte)(rleWord >> 8));
                            result.Add((byte)rleWord);
                            Reset();
                            continue;
                        }
                    }
                    else
                    {
                        if (literalCount > rleByteCount)
                            WriteLiterals(rleByteCount);
                    }

                    Increment(false);
                }

                if (rleByteCount > 3)
                {
                    result.Add(unchecked((byte)(rleByteCount - 103)));
                    result.Add(rleByte);
                }
                else if (rleDwordCount > 1)
                {
                    result.Add(unchecked((byte)(rleDwordCount - 130)));
                    result.Add((byte)(rleDword >> 24));
                    result.Add((byte)(rleDword >> 16));
                    result.Add((byte)(rleDword >> 8));
                    result.Add((byte)rleDword);
                }
                else if (rleWord > 1)
                {
                    result.Add(unchecked((byte)(rleWordCount - 116)));
                    result.Add((byte)(rleWord >> 8));
                    result.Add((byte)rleWord);
                }
                else if (rleByteCount == 3)
                {
                    result.Add(unchecked((byte)-100));
                    result.Add(rleByte);
                }
                else if (literalCount > 0)
                {
                    WriteLiterals();
                }
            }

            return result.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            List<byte> result = new List<byte>(data.Length);

            for (int i = 0; i < data.Length; ++i)
            {
                sbyte header = (sbyte)data[i];

                if (header > 0)
                {
                    for (int j = 0; j < header; ++j)
                        result.Add(data[i++]);
                }
                else if (header >= -100)
                {
                    int numBytes = 103 + header;
                    byte rleByte = data[i++];

                    for (int j = 0; j < numBytes; ++j)
                        result.Add(rleByte);
                }
                else if (header >= -114)
                {
                    int numWords = 116 + header;
                    byte rleWordByte1 = data[i++];
                    byte rleWordByte2 = data[i++];

                    for (int j = 0; j < numWords; ++j)
                    {
                        result.Add(rleWordByte1);
                        result.Add(rleWordByte2);
                    }
                }
                else
                {
                    int numDwords = 130 + header;
                    byte rleDwordByte1 = data[i++];
                    byte rleDwordByte2 = data[i++];
                    byte rleDwordByte3 = data[i++];
                    byte rleDwordByte4 = data[i++];

                    for (int j = 0; j < numDwords; ++j)
                    {
                        result.Add(rleDwordByte1);
                        result.Add(rleDwordByte2);
                        result.Add(rleDwordByte3);
                        result.Add(rleDwordByte4);
                    }
                }
            }

            return result.ToArray();
        }
    }
}
