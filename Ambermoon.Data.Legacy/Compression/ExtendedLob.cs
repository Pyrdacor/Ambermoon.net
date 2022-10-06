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
        const int MaxLargeMatchLength = 3 + 0x1f;
        const int MaxSmallMatchOffset = 0x1ff;
        const int MaxLargeMatchOffset = 0xffff;

        public static byte[] CompressData(byte[] data)
        {
            var literals = new List<byte>(127);
            int rleCount = 0;
            byte rleLiteral = 0;
            var compressedData = new List<byte>(data.Length);
            var trie = new MatchTrie(MaxLargeMatchOffset);
            compressedData.Add(0); // add header dummy
            int i = 0;

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

            void WriteCurrentData(bool nextIsLiteral)
            {
                if (rleCount >= 3)
                {
                    if (rleLiteral == 0)
                    {
                        while (rleCount >= 3)
                        {
                            int count = Math.Min(rleCount, 258);
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
                            compressedData.Add(0);
                            compressedData.Add((byte)(count - 3));
                            rleCount -= count;
                        }
                    }

                    while (rleCount-- != 0)
                        literals.Add(rleLiteral);
                }
                
                if (!nextIsLiteral && literals.Count != 0)
                {
                    while (literals.Count != 0)
                    {
                        if (!literals.Any(l => l > 31))
                            literals.ForEach(l => compressedData.Add((byte)(l | 0xe0)));
                        else
                        {
                            int count = Math.Min(127, literals.Count);
                            var literalsToEncode = literals.Take(count).ToList();
                            compressedData.Add((byte)count);
                            literalsToEncode.ForEach(compressedData.Add);
                            literals.RemoveRange(0, count);
                        }
                    }
                }

                // TODO: matches
            }

            if (CheckRle())
            {
                i += 3;
            }
            else
            {
                trie.Add(data, 0, MaxLargeMatchLength);
                literals.Add(data[i++]);
            }

            for (; i <= data.Length - MaxLargeMatchLength; ++i)
            {
                var match = trie.GetLongestMatch(data, i, MaxMatchLength);

                trie.Add(data, i, MaxMatchLength);

                if (match.Value > 2)
                {
                    AddMatch(i - match.Key, match.Value, i + match.Value == data.Length);

                    for (int j = 1; j < match.Value; ++j)
                        trie.Add(data, i + j, Math.Min(MaxMatchLength, data.Length - i - j));

                    i += match.Value - 1; // -1 cause of for's ++i
                }
                else
                    AddByte(data[i]);
            }

            for (; i <= data.Length - MinMatchLength; ++i)
            {
                int length = data.Length - i;
                var match = trie.GetLongestMatch(data, i, length);

                trie.Add(data, i, length);

                if (match.Value > 2)
                {
                    AddMatch(i - match.Key, match.Value, i + match.Value == data.Length);

                    for (int j = 1; j < match.Value; ++j)
                        trie.Add(data, i + j, length - j);

                    i += match.Value - 1; // -1 cause of for's ++i
                }
                else
                    AddByte(data[i]);
            }

            for (; i < data.Length; ++i)
            {
                AddByte(data[i], i == data.Length - 1);
            }

            if (currentHeaderBitMask != 0x80)
                compressedData[currentHeaderPosition] = currentHeader;

            return compressedData.ToArray();
        }

        public static DataReader Decompress(IDataReader reader, uint decodedSize)
        {
            var decodedData = new byte[decodedSize];
            uint decodeIndex = 0;
            uint matchOffset;
            uint matchLength;
            uint matchIndex;

            while (decodeIndex < decodedSize)
            {
                byte header = reader.ReadByte();

                for (int i = 0; i < 8; ++i)
                {
                    if ((header & 0x80) == 0) // match
                    {
                        matchOffset = reader.ReadByte();
                        matchLength = (matchOffset & 0x000f) + 3;
                        matchOffset <<= 4;
                        matchOffset &= 0xff00;
                        matchOffset |= reader.ReadByte();
                        matchIndex = decodeIndex - matchOffset;

                        while (matchLength-- != 0)
                        {
                            decodedData[decodeIndex++] = decodedData[matchIndex++];
                        }
                    }
                    else // normal byte
                    {
                        decodedData[decodeIndex++] = reader.ReadByte();
                    }

                    if (decodeIndex == decodedSize)
                        break;

                    header <<= 1;
                }
            }

            return new DataReader(decodedData);
        }
    }
}
