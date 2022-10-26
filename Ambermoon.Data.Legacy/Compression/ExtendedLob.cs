using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Compression
{
    internal static class ExtendedLob
    {
        const int MinMatchLength = 3;
        const int MaxMatchLength = 32;
        const int MinMatchOffset = 1;
        const int MaxMatchOffset = 1024;
        const int MinRLELength = 4;

        public static byte[] CompressData(byte[] data)
        {
            var literals = new List<byte>();
            int rleCount = 0;
            byte rleLiteral = 0;
            int lastMatchHeaderIndex = -1;
            var compressedData = new List<byte>(data.Length);
            var trie = new MatchTrie(MaxMatchOffset);
            int i = 0;
            bool justFoundRle = false;
            int lastLiteral = -1;

            void WriteAdditionalCount(int additionalCount)
            {
                int count;

                do
                {
                    count = Math.Min(additionalCount, 255);
                    compressedData.Add((byte)count);
                    additionalCount -= count;
                } while (count == 255);
            }

            bool WriteMoreLiterals()
            {
                int consumedCount = Math.Min(255, literals.Count);
                compressedData.Add((byte)consumedCount);
                for (int i = 0; i < consumedCount; ++i)
                    compressedData.Add(literals[i]);
                if (consumedCount == literals.Count)
                    literals.Clear();
                else
                    literals = literals.Skip(consumedCount).ToList();
                return consumedCount == 255;
            }

            bool CheckRle()
            {
                if (data.Length - i >= MinRLELength &&
                    data[i] == data[i + 1] &&
                    data[i] == data[i + 2] &&
                    data[i] == data[i + 3])
                {
                    rleLiteral = data[i];
                    rleCount = MinRLELength;
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
                        if (++length == MaxMatchLength)
                            break; // enough for our purposes
                    }
                    else
                        break;
                }

                return length;
            }

            void WriteCurrentData(bool nextIsLiteral, bool noRle = false, byte? useRleLiteral = null)
            {
                void ProcessLiterals()
                {
                    int remainingLiteralCount = literals.Count;

                    if (remainingLiteralCount == 0)
                        return;

                    int firstLiteralCount;

                    // Add literal amount of 0 to 3 in the last match encoding.
                    if (lastMatchHeaderIndex != -1)
                    {
                        firstLiteralCount = Math.Min(3, remainingLiteralCount);

                        compressedData[lastMatchHeaderIndex] |= (byte)(firstLiteralCount << 2);

                        if (remainingLiteralCount <= 3)
                        {
                            // Fits into the last match encoding
                            if (literals.Count != 0)
                            {
                                literals.ForEach(l => compressedData.Add(l));
                                lastLiteral = literals[^1];
                                literals.Clear();
                            }
                            return;
                        }
                        else
                        {
                            foreach (var literal in literals.Take(firstLiteralCount))
                                compressedData.Add(literal);

                            literals = literals.Skip(firstLiteralCount).ToList();
                        }

                        remainingLiteralCount -= firstLiteralCount;
                    }
                    else
                    {
                        // This is the first sequence of literals in the data. It has a special encoding.
                        if (literals.Count == 0)
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid extended lob data.");

                        if (literals.Count < 256)
                        {
                            compressedData.Add((byte)literals.Count);
                            literals.ForEach(l => compressedData.Add(l));
                            lastLiteral = literals[^1];
                            literals.Clear();
                            return;
                        }
                        else
                        {
                            compressedData.Add(0);
                            remainingLiteralCount -= 256;
                            for (int i = 0; i < 256; ++i)
                                compressedData.Add(literals[i]);
                            literals = literals.Skip(256).ToList();
                        }

                        lastLiteral = literals[^1];
                        bool moreLiterals = WriteMoreLiterals();

                        while (moreLiterals)
                            moreLiterals = WriteMoreLiterals();

                        return;
                    }

                    // Only now we have to add a new literal encoding.
                    firstLiteralCount = Math.Min(remainingLiteralCount, 32);
                    compressedData.Add((byte)(0xe0 | (firstLiteralCount - 1))); // Write literal header
                    remainingLiteralCount -= firstLiteralCount;
                    lastLiteral = literals[^1];
                    for (int i = 0; i < firstLiteralCount; ++i)
                        compressedData.Add(literals[i]);
                    literals = literals.Skip(firstLiteralCount).ToList();

                    if (firstLiteralCount == 32)
                    {
                        bool moreLiterals = WriteMoreLiterals();

                        while (moreLiterals)
                            moreLiterals = WriteMoreLiterals();
                    }
                }

                if (rleCount >= MinRLELength && !noRle)
                {
                    int index = i - rleCount;
                    int addTrieCount = rleCount;

                    if (index < MinRLELength)
                    {
                        int reduce = MinRLELength - index;
                        addTrieCount -= reduce;
                        index += reduce;
                    }

                    for (int j = 0; j < addTrieCount; ++j)
                        trie.Add(data, index + j, Math.Min(MaxMatchLength, data.Length - index - j));

                    byte literal = useRleLiteral ?? rleLiteral;
                    bool rleLiteralAlreadyThere = (literals.Count != 0 && literals[^1] == literal) ||
                        (literals.Count == 0 && lastLiteral == literal);

                    if (!rleLiteralAlreadyThere)
                    {
                        --rleCount; // match count does not include the first literal
                        literals.Add(literal); // it is part of the preceeding literals instead
                    }

                    ProcessLiterals();
                    lastLiteral = literal;
                    lastMatchHeaderIndex = compressedData.Count;

                    int firstCount = Math.Min(rleCount, 16);
                    compressedData.Add((byte)((firstCount - 3) << 4));
                    compressedData.Add(0); // offset 1
                    rleCount -= firstCount;

                    if (firstCount == 16)
                    {
                        WriteAdditionalCount(rleCount);
                        rleCount = 0;
                    }

                    return;
                }
                
                if (!nextIsLiteral && literals.Count != 0)
                {
                    ProcessLiterals();
                }
            }

            trie.Add(data, 0, MaxMatchLength);
            literals.Add(data[i++]);

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
                    i += MinRLELength - 1;
                    continue;
                }
                else if (rleCount >= MinRLELength)
                {
                    int rleCountBackup = rleCount;
                    byte rleLiteralBackup = rleLiteral;

                    if (CheckRle())
                    {
                        justFoundRle = true;
                        rleCount = rleCountBackup;
                        WriteCurrentData(false, false, rleLiteralBackup);
                        rleCount = MinRLELength;
                    }
                }

                void AddMatch(int offset, int length)
                {
                    int firstMatchLength = Math.Min(16, length);
                    length -= firstMatchLength;
                    --offset;
                    byte header = (byte)(((firstMatchLength - 3) << 4) | (offset >> 8));
                    lastMatchHeaderIndex = compressedData.Count;
                    compressedData.Add(header);
                    compressedData.Add((byte)(offset & 0xff));

                    if (firstMatchLength == 16)
                        WriteAdditionalCount(length);
                }

                int maxMatchLength = Math.Min(data.Length - i, MaxMatchLength);
                var match = trie.GetLongestMatch(data, i, maxMatchLength);
                int rleLength = justFoundRle ? CheckRleLength(data, i) : 0;
                int matchOffset = i - match.Key;

                if (matchOffset >= MinMatchOffset && matchOffset <= MaxMatchOffset && match.Value >= MinMatchLength && match.Value > rleLength)
                {
                    trie.Add(data, i, maxMatchLength);

                    if (!justFoundRle)
                        WriteCurrentData(false);
                    else
                        rleCount = 0;

                    lastLiteral = data[match.Key + match.Value - 1];

                    AddMatch(i - match.Key, match.Value);

                    for (int j = 1; j < match.Value; ++j)
                        trie.Add(data, i + j, Math.Min(MaxMatchLength, data.Length - i - j));

                    i += match.Value - 1; // -1 cause of for's ++i
                }
                else if (justFoundRle)
                {
                    i += MinRLELength - 1;
                }
                else
                {
                    if (rleCount < MinRLELength)
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

            int ReadAdditionalCount(int initialCount)
            {
                int count;

                do
                {
                    count = reader.ReadByte();
                    initialCount += count;
                } while (count == 255);

                return initialCount;
            }

            int literalCount = reader.ReadByte();
            bool moreCountBytes = false;

            if (literalCount == 0)
            {
                literalCount = 256;
                moreCountBytes = true;
            }

            bool ReadNextCount()
            {
                if (!moreCountBytes)
                    return false;

                literalCount = reader.ReadByte();
                moreCountBytes = literalCount == 255;

                return true;
            }

            do
            {
                for (int i = 0; i < literalCount; ++i)
                    decodedData[decodeIndex++] = reader.ReadByte();
            } while (ReadNextCount());

            while (decodeIndex < decodedSize)
            {
                ushort header = reader.ReadWord();

                if ((header & 0xe000) != 0xe000) // match
                {
                    int offset = (header & 0x3ff) + 1;
                    int length = (header >> 12) + 3;
                    literalCount = (header >> 10) & 0x3;

                    if (length == 16)
                        length = ReadAdditionalCount(16);

                    int sourceIndex = (int)decodeIndex - offset;

                    for (int i = 0; i < length; ++i)
                        decodedData[decodeIndex++] = decodedData[sourceIndex++];

                    for (int i = 0; i < literalCount; ++i)
                        decodedData[decodeIndex++] = reader.ReadByte();
                }
                else
                {
                    literalCount = (header >> 8) & 0x1f;
                    moreCountBytes = literalCount == 31;
                    decodedData[decodeIndex++] = (byte)(header & 0xff);

                    do
                    {
                        for (int i = 0; i < literalCount; ++i)
                            decodedData[decodeIndex++] = reader.ReadByte();
                    } while (ReadNextCount());
                }
            }

            if (reader.Position % 2 != 0 && reader.Position < reader.Size)
                ++reader.Position;

            return new DataReader(decodedData);
        }
    }
}
