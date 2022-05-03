using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Compression
{
    public static class Lob
    {
        const int MinMatchLength = 3;
        const int MaxMatchLength = 18;

        public static byte[] CompressData(byte[] data)
        {
            // ease algorithm by not compressing very small data (20 bytes)
            if (data.Length < MaxMatchLength + 2)
                return data;

            var compressedData = new List<byte>(data.Length);
            var trie = new MatchTrie();
            int currentHeaderPosition = 0;
            byte currentHeaderBitMask = 0x80 >> 1; // skip first bit
            byte currentHeader = 0x80; // first entry/byte is no match
            compressedData.Add(0); // add header dummy
            int i = 1;

            void AddByte(byte b, bool last = false)
            {
                currentHeader |= currentHeaderBitMask;
                compressedData.Add(b);
                PostAdd(last);
            }

            void AddMatch(int offset, int length, bool last = false)
            {
                byte b1 = (byte)(((offset >> 4) & 0xf0) | ((length - 3) & 0x0f));
                compressedData.Add(b1);
                compressedData.Add((byte)(offset & 0xff));
                PostAdd(last);
            }

            void PostAdd(bool last)
            {
                currentHeaderBitMask >>= 1;

                if (currentHeaderBitMask == 0)
                {
                    compressedData[currentHeaderPosition] = currentHeader;
                    currentHeaderBitMask = 0x80;
                    currentHeader = 0;

                    if (!last)
                    {
                        currentHeaderPosition = compressedData.Count;
                        compressedData.Add(0); // new header
                    }
                    else if (compressedData.Count % 2 == 1)
                    {
                        compressedData.Add(0xc0); // new header
                        compressedData.Add(0); // padding byte
                        compressedData.Add(0); // padding byte
                    }
                }
                else if (last && compressedData.Count % 2 == 1)
                {
                    compressedData.Add(0); // padding byte
                }
            }

            // first byte can not contain a match
            trie.Add(data, 0, MaxMatchLength);
            compressedData.Add(data[0]);

            for (; i <= data.Length - MaxMatchLength; ++i)
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
