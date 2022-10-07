using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class ExtendedLob
    {
        const int MinMatchLength = 3;
        const int MaxSmallMatchLength = 3 + 0xf;
        const int MaxLargeMatchLength = 3 + 0x7f;
        const int MaxSmallMatchOffset = 0x1ff;
        const int MaxLargeMatchOffset = 0x3ff;

        // Small match: 100LLLLO OOOOOOOO, Length = 0000 (3) to 1111 (18), Offset = 000000000 (0) to 111111111 (511)
        // Large match: 100LLLLL LLOOOOOO OOOO, Length = 0000000 (3) to 1111111 (130), Offset = 0000000000 (0) to 1111111111 (1023)

        public static byte[] CompressData(byte[] data)
        {
            var literals = new List<byte>(127);
            int rleCount = 0;
            byte rleLiteral = 0;
            int largeMatchReserveIndex = -1;
            var compressedData = new List<byte>(data.Length);
            var trie = new MatchTrie(MaxLargeMatchOffset);
            int i = 0;
            bool justFoundRle = false;

            void Debug(string text)
            {
                string enctext = $"{i:x4}: {text}";
                System.IO.File.AppendAllText(@"C:\Users\flavia\Downloads\ExtLobTests\enc.txt", enctext + Environment.NewLine);
                text = $"{compressedData.Count:000}: {text}";
                System.IO.File.AppendAllText(@"C:\Users\flavia\Downloads\ExtLobTests\encode.txt", text + Environment.NewLine);
            }

            bool CheckRle()
            {
                if (data.Length - i >= 3 &&
                    data[i] == data[i + 1] &&
                    data[i] == data[i + 2])
                {
                    rleLiteral = data[i];
                    rleCount = 3;
                    return true;
                }

                return false;
            }

            int CheckRleLength(byte[] data, int index)
            {
                int length = 1;
                byte literal = data[index];

                for (int i = index + 1; i < data.Length; ++i)
                {
                    if (data[i] == literal)
                    {
                        if (++length == 34)
                            break; // enough for our purposes
                    }
                    else
                        break;
                }

                return length;
            }

            void WriteCurrentData(bool nextIsLiteral, bool noRle = false, byte? useRleLiteral = null)
            {
                if (rleCount >= 3 && !noRle)
                {
                    int index = i - rleCount;

                    for (int j = 0; j < rleCount; ++j)
                        trie.Add(data, index + j, Math.Min(MaxLargeMatchLength, data.Length - index - j));

                    byte literal = useRleLiteral ?? rleLiteral;

                    if (literal == 0)
                    {
                        while (rleCount >= 3)
                        {
                            int count = Math.Min(rleCount, 258);
                            Debug("RLE0," + count.ToString());
                            compressedData.Add(0);
                            compressedData.Add((byte)(count - 3));
                            rleCount -= count;
                        }
                    }
                    else
                    {
                        while (rleCount >= 3)
                        {
                            int count = Math.Min(rleCount, 34);
                            Debug("RLE" + ((int)literal).ToString() + "," + count.ToString());
                            compressedData.Add((byte)(0xc0 | (count - 3)));
                            compressedData.Add(literal);
                            rleCount -= count;
                        }
                    }

                    while (rleCount != 0)
                    {
                        literals.Add(rleLiteral);
                        --rleCount;
                    }
                }
                
                if (!nextIsLiteral && literals.Count != 0)
                {
                    while (literals.Count != 0)
                    {
                        if (!literals.Any(l => l > 31))
                        {
                            literals.ForEach(_ => Debug("SmallLiteral"));
                            literals.ForEach(l => compressedData.Add((byte)(l | 0xe0)));
                            literals.Clear();
                            break;
                        }
                        else
                        {
                            int count = Math.Min(127, literals.Count);
                            Debug("Literals," + count);
                            var literalsToEncode = literals.Take(count).ToList();
                            compressedData.Add((byte)count);
                            literalsToEncode.ForEach(compressedData.Add);
                            literals.RemoveRange(0, count);
                        }
                    }
                }
            }

            if (CheckRle())
            {
                trie.Add(data, 0, Math.Min(MaxLargeMatchLength, data.Length));
                trie.Add(data, 1, Math.Min(MaxLargeMatchLength, data.Length - 1));
                trie.Add(data, 2, Math.Min(MaxLargeMatchLength, data.Length - 2));
                i += 3;
            }
            else
            {
                trie.Add(data, 0, MaxLargeMatchLength);
                literals.Add(data[i++]);
            }

            for (; i < data.Length; ++i)
            {
                justFoundRle = false;

                if (rleCount != 0 && data[i] == rleLiteral)
                {
                    ++rleCount;
                    continue;
                }
                else if (rleCount == 0 && CheckRle())
                {
                    justFoundRle = true;
                    WriteCurrentData(false, true);
                }
                else if (rleCount >= 3)
                {
                    int rleCountBackup = rleCount;
                    byte rleLiteralBackup = rleLiteral;

                    if (CheckRle())
                    {
                        justFoundRle = true;
                        rleCount = rleCountBackup;
                        WriteCurrentData(false, false, rleLiteralBackup);
                        rleCount = 3;
                    }
                }

                void AddMatch(int offset, int length)
                {
                    if (length > MaxSmallMatchLength || offset > MaxSmallMatchOffset)
                    {
                        // large match
                        length -= MinMatchLength;
                        compressedData.Add((byte)(0xa0 | (length >> 2)));
                        compressedData.Add((byte)(((length & 0x3) << 6)|(offset >> 4)));
                        if (largeMatchReserveIndex == -1)
                        {
                            largeMatchReserveIndex = compressedData.Count;
                            compressedData.Add((byte)((offset & 0xf) << 4));
                        }
                        else
                        {
                            compressedData[largeMatchReserveIndex] |= (byte)(offset & 0xf);
                            largeMatchReserveIndex = -1;
                        }
                    }
                    else
                    {
                        // small match
                        int b1 = 0x80 | ((length - MinMatchLength) << 1);
                        if (offset > 255)
                            ++b1;
                        compressedData.Add((byte)b1);
                        compressedData.Add((byte)(offset & 0xff));
                    }
                }

                int maxMatchLength = Math.Min(data.Length - i, MaxLargeMatchLength);
                var match = trie.GetLongestMatch(data, i, maxMatchLength);
                int rleLength = justFoundRle ? CheckRleLength(data, i) : 0;

                if (match.Value >= MinMatchLength && match.Value > rleLength)
                {
                    trie.Add(data, i, maxMatchLength);

                    if (!justFoundRle)
                        WriteCurrentData(false);
                    else
                        rleCount = 0;
                    Debug("Match[" + (i - match.Key) + "," + match.Value + "]");
                    AddMatch(i - match.Key, match.Value);

                    for (int j = 1; j < match.Value; ++j)
                        trie.Add(data, i + j, Math.Min(MaxLargeMatchLength, data.Length - i - j));

                    i += match.Value - 1; // -1 cause of for's ++i
                }
                else if (justFoundRle)
                {
                    i += 2;
                }
                else
                {
                    trie.Add(data, i, maxMatchLength);
                    WriteCurrentData(true);
                    literals.Add(data[i]);
                }
            }

            WriteCurrentData(false);

            if (compressedData.Count % 2 != 0)
                compressedData.Add(0);

            return compressedData.ToArray();
        }

        public static DataReader Decompress(IDataReader reader, uint decodedSize)
        {
            var decodedData = new byte[decodedSize];
            uint decodeIndex = 0;
            bool useLargeMatchReserve = false;
            byte largeMatchReserve = 0;

            void Debug(int index, string text)
            {
                text = $"{index:000}: {text}";
                System.IO.File.AppendAllText(@"C:\Users\flavia\Downloads\ExtLobTests\decode.txt", text + Environment.NewLine);
            }

            while (decodeIndex < decodedSize)
            {
                int position = reader.Position;
                byte header = reader.ReadByte();

                if (header == 0)
                {
                    int amount = reader.ReadByte() + 3;
                    Debug(position, "RLE0," + amount);

                    for (int i = 0; i < amount; ++i)
                        decodedData[decodeIndex++] = 0;
                }
                else if (header < 128)
                {
                    Debug(position, "Literals," + (int)header);

                    for (int i = 0; i < header; ++i)
                        decodedData[decodeIndex++] = reader.ReadByte();
                }
                else
                {
                    int mode = (header >> 5) & 3;                    

                    void ProcessMatch(int offset, int length)
                    {
                        Debug(position, "Match[" + offset + "," + length + "]");
                        int sourceIndex = (int)decodeIndex - offset;

                        for (int i = 0; i < length; ++i)
                            decodedData[decodeIndex++] = decodedData[sourceIndex++];
                    }

                    if (mode == 0) // small match
                    {
                        int length = ((header >> 1) & 0xf) + 3;
                        int offset = header & 0x1;
                        offset <<= 8;
                        offset |= reader.ReadByte();
                        ProcessMatch(offset, length);
                    }
                    else if (mode == 1) // large match
                    {
                        int length = (header & 0x1f) << 2;
                        int offset = reader.ReadByte();
                        length |= (offset >> 6);
                        length += 3;
                        offset &= 0x3f;
                        offset <<= 4;
                        if (useLargeMatchReserve)
                            offset |= largeMatchReserve;
                        else
                        {
                            largeMatchReserve = reader.ReadByte();
                            offset |= (largeMatchReserve >> 4);
                            largeMatchReserve &= 0xf;
                        }
                        useLargeMatchReserve = !useLargeMatchReserve;
                        ProcessMatch(offset, length);
                    }
                    else if (mode == 2) // literal rle
                    {
                        int length = (header & 0x1f) + 3;
                        byte literal = reader.ReadByte();
                        Debug(position, "RLE" + (int)literal + "," + length);

                        for (int i = 0; i < length; ++i)
                            decodedData[decodeIndex++] = literal;
                    }
                    else // mode == 3, small literal
                    {
                        Debug(position, "SmallLiteral");
                        decodedData[decodeIndex++] = (byte)(header & 0x1f);
                    }
                }
            }

            if (reader.Position % 2 != 0 && reader.Position < reader.Size)
                ++reader.Position;

            return new DataReader(decodedData);
        }
    }
}
