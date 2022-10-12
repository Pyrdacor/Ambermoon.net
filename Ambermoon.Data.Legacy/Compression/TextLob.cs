using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Compression
{
    // As the original only loads one subfile at a time
    // the compression must be focused on single subfiles
    // as well. Multiple texts are still packed into
    // one subfile. For example all texts for map 257 are
    // inside the subfile 257 of 2Map_texts.amb.
    // Texts won't contain RLE sequences or very long
    // matches so the focus should be short matches within
    // a given range. The largest subfiles are at around
    // 7KB but most are somewhere between 1 and 3 KB.
    // So the max match offset should fall into this range.
    // As there will be many 3 letter matches or even 2
    // letter matches, it is crucial to encode the matches
    // very small. Luckily characters 1 to 31 are not used
    // so we can use them to encode part of the match.
    //
    // The first byte is checked. If it is >= 32, it
    // is just copied as is. If it is 0x1f (31) a 0
    // is written. Otherwise the next byte is read as well.
    //
    // So we have values 0 (00) to 30 (1e) for matches.
    //
    // 00000 to 11110
    //
    // The msb of them specifies the match type:
    // - 0: 2 byte match
    // - 1: 3 to 10 byte match
    //
    // 2 byte matches only use 1.5 bytes (12 bits in total).
    //
    // 0000OOOO OOOO
    //
    // The offset is limited to 00000000 to 11111111.
    // This is interpreted as 3 to 258.
    //
    // As only 4 bits of the next byte is used, the remaining
    // 4 bits are used for the next 2-byte-match.
    //
    // Longer matches use 2 bytes (16 bits in total).
    // 
    // 0001OOOO OOOOOLLL
    //
    // The offset is limited to 000000000 to 111011111.
    // It is interpreted as 3 to 482.
    //
    // The length is limited to 000 to 111.
    // It is interpreted as 3 to 10.
    //
    // As text files may start with some header, we skip
    // all parts where at least 1 byte is in the invalid range.
    // The encoded file starts with a byte which gives the amount
    // of bytes to skip. If this value would be greater than 255
    // encoding will abort.
    internal static class TextLob
    {
        const int MaxMatchLength = 10;
        const int MinMatchOffset = 3;
        const int MaxSmallMatchOffset = 258;
        const int MaxMatchOffset = 482;

        public static byte[] CompressData(byte[] data)
        {
            var compressedData = new List<byte>(data.Length);
            var trie = new MatchTrie(MaxMatchOffset);
            int i = 0;
            int matchReserveIndex = -1;

            void AddMatch(int offset, int length)
            {
                if (length < 1 || length > MaxMatchLength)
                    throw new Exception("Length " + length);
                if (offset < MinMatchOffset || offset > MaxMatchOffset)
                    throw new Exception("Offset " + offset);
                if (length == 2 && offset > MaxSmallMatchOffset)
                    throw new Exception("2 Offset " + offset);

                offset -= MinMatchOffset;

                if (length == 2)
                {
                    compressedData.Add((byte)(offset >> 4));

                    if (matchReserveIndex != -1)
                    {
                        compressedData[matchReserveIndex] |= (byte)(offset & 0xf);
                        matchReserveIndex = -1;
                    }
                    else
                    {
                        matchReserveIndex = compressedData.Count;
                        compressedData.Add((byte)((offset & 0xf) << 4));
                    }
                }
                else
                {
                    compressedData.Add((byte)(0x10 | (offset >> 5)));
                    compressedData.Add((byte)(((offset & 0x1f) << 3) | (length - 3)));
                }
            }

            void CheckDataValidity(int index)
            {
                if (data[index] > 0 && data[index] < 32)
                    throw new AmbermoonException(ExceptionScope.Data, $"Unsupported text data at index {index}.");
            }

            int skipBytes = data.ToList().FindLastIndex(b => b > 0 && b < 32) + 1;

            if (skipBytes > 255)
                throw new AmbermoonException(ExceptionScope.Data, "Data can't be compressed with text lob.");

            compressedData.Add((byte)skipBytes);            

            for (; i < skipBytes; ++i)
                compressedData.Add(data[i]);

            // first byte can not contain a match
            CheckDataValidity(i);
            trie.Add(data, i, MaxMatchLength);
            if (data[i] == 0)
            {
                compressedData.Add(0x1f);
                ++i;
            }
            else
                compressedData.Add(data[i++]);

            for (; i < data.Length; ++i)
            {
                CheckDataValidity(i);
                int maxMatchLength = Math.Min(MaxMatchLength, data.Length - i);
                var match = trie.GetLongestMatch(data, i, maxMatchLength);

                trie.Add(data, i, maxMatchLength);

                int matchOffset = i - match.Key;
                int matchLength = match.Value;

                if (matchOffset >= MinMatchOffset && matchOffset <= MaxMatchOffset && (matchLength > 2 || (matchLength > 1 && matchOffset <= MaxSmallMatchOffset)))
                {
                    AddMatch(matchOffset, matchLength);

                    for (int j = 1; j < matchLength; ++j)
                        trie.Add(data, i + j, Math.Min(MaxMatchLength, data.Length - i - j));

                    i += matchLength - 1; // -1 cause of for's ++i
                }
                else if (data[i] == 0)
                {
                    compressedData.Add(0x1f);
                }
                else
                {
                    compressedData.Add(data[i]);
                }
            }

            if (compressedData.Count % 2 != 0)
                compressedData.Add(0);

            return compressedData.ToArray();
        }

        public static DataReader Decompress(IDataReader reader, uint decodedSize)
        {
            var decodedData = new byte[decodedSize];
            int decodeIndex = 0;
            int matchOffset;
            int matchLength;
            int matchIndex;
            byte matchReserve = 0;
            bool useMatchReserve = false;

            int start = reader.Position;
            byte skipBytes = reader.ReadByte();

            for (int i = 0; i < skipBytes; ++i)
                decodedData[decodeIndex++] = reader.ReadByte();

            while (decodeIndex < decodedSize)
            {
                byte header = reader.ReadByte();

                if (header == 31)
                {
                    decodedData[decodeIndex++] = 0;
                }
                else if (header >= 32)
                {
                    decodedData[decodeIndex++] = header;
                }
                else // match
                {
                    bool longMatch = (header & 0x10) != 0;
                    if (longMatch)
                    {
                        matchLength = reader.ReadByte();
                        matchOffset = matchLength >> 3;
                        matchLength &= 0x7;
                        matchLength += 3;
                        matchOffset |= ((header & 0xf) << 5);
                    }
                    else
                    {
                        matchLength = 2;
                        matchOffset = (header & 0xf) << 4;

                        if (useMatchReserve)
                            matchOffset |= matchReserve & 0xf;
                        else
                        {
                            matchReserve = reader.ReadByte();
                            matchOffset |= matchReserve >> 4;
                        }    

                        useMatchReserve = !useMatchReserve;
                    }

                    matchOffset += MinMatchOffset;
                    matchIndex = decodeIndex - matchOffset;

                    while (matchLength-- != 0)
                    {
                        decodedData[decodeIndex++] = decodedData[matchIndex++];
                    }
                }
            }

            if ((reader.Position - start) % 2 != 0)
                ++reader.Position; // skip align byte

            return new DataReader(decodedData);
        }
    }
}
