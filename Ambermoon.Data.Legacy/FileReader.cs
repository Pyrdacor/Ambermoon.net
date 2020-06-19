using System;
using System.Collections.Generic;
using System.IO;

namespace Ambermoon.Data.Legacy
{
    public class FileReader : IFileReader
    {
        private struct FileInfo
        {
            public FileType FileType;
            public int NumFiles;
            public bool SingleFile;
        }

        private class FileContainer : IFileContainer
        {
            public string Name { get; set; }
            public Dictionary<int, IDataReader> Files { get; } = new Dictionary<int, IDataReader>();
        }

        public IFileContainer ReadFile(string name, Stream stream)
        {
            byte[] rawData = new byte[stream.Length - stream.Position];
            stream.Read(rawData, 0, rawData.Length);

            var reader = new DataReader(rawData);
            var header = reader.ReadDword();
            var fileType = ((header & 0xffff0000) == (uint)FileType.JH) ? FileType.JH : (FileType)header;
            var fileInfo = new FileInfo { FileType = fileType };

            switch (fileType)
            {
                case FileType.JH:
                case FileType.LOB:
                case FileType.VOL1:
                    fileInfo.SingleFile = true;
                    fileInfo.NumFiles = 1;
                    break;
                case FileType.AMNC:
                case FileType.AMNP:
                case FileType.AMBR:
                case FileType.AMPC:
                    fileInfo.SingleFile = false;
                    fileInfo.NumFiles = reader.ReadWord();
                    break;
                default: // raw format
                    var fileContainer = new FileContainer { Name = name };
                    fileContainer.Files.Add(1, new DataReader(rawData));
                    return fileContainer;
            }

            reader.Position = 0;

            return ProcessFileInfo(name, fileInfo, reader);
        }

        private IFileContainer ProcessFileInfo(string name, FileInfo fileInfo, DataReader reader)
        {
            var fileContainer = new FileContainer { Name = name };

            if (fileInfo.SingleFile)
            {
                fileContainer.Files.Add(1, DecodeFile(reader, fileInfo.FileType, 1));
            }
            else
            {
                reader.Position = 6; // here the file sizes follow (dword each)
                int offset = 6 + (fileInfo.NumFiles << 2); // 4 bytes per size entry

                for (int i = 1; i <= fileInfo.NumFiles; ++i)
                {
                    int size = (int)reader.ReadDword();

                    if (size > 0)
                    {
                        fileContainer.Files.Add(i, DecodeFile(new DataReader(reader, offset, size), fileInfo.FileType, i));
                        offset += size;
                    }
                }
            }

            return fileContainer;
        }

        private DataReader DecodeFile(DataReader reader, FileType containerType, int fileNumber)
        {
            var header = reader.PeekDword();
            var fileType = ((header & 0xffff0000) == (uint)FileType.JH) ? FileType.JH : (FileType)header;

            if (fileType == FileType.JH)
            {
                reader.Position += 4; // skip header
                reader = new DataReader(DecryptJHFile(reader, (ushort)((((uint)fileType & 0xffff0000) >> 16) ^ ((uint)fileType & 0x0000ffff))));
            }
            else if (containerType == FileType.AMNC) // AMNC archives are always encoded
            {
                reader = new DataReader(DecryptJHFile(reader, (ushort)fileNumber));
            }

            header = reader.PeekDword(); // Note: The reader might have changed above.
            fileType = (FileType)header; // Note: No need to check for JH here as this can not happen.

            // See if it is a LOB file
            if (fileType == FileType.LOB || fileType == FileType.VOL1)
            {
                reader.Position += 4; // skip header
                uint decodedSize = reader.PeekDword() & 0x00ffffff; // the first byte would contain the offset for the size entry

                // AMNP archives are always encoded
                if (containerType == FileType.AMNP)
                {
                    reader.Position += 4; // skip decoded size
                    reader = new DataReader(DecryptJHFile(reader, (ushort)fileNumber));
                    reader.Position += 4; // skip encoded size
                }
                else
                {
                    reader.Position += 8;  // skip decoded and encoded size
                }

                return UnLOB(reader, decodedSize);
            }
            else
            {
                // AMNP archives are always encoded
                if (containerType == FileType.AMNP)
                {
                    return new DataReader(DecryptJHFile(reader, (ushort)fileNumber, 4));
                }
                else
                {
                    return reader;
                }
            }            
        }

        private static DataReader UnLOB(DataReader reader, uint decodedSize)
        {
            unsafe
            {
                var decodedData = new byte[decodedSize];

                fixed (byte* dataPtr = decodedData)
                {
                    byte* dstPtr = dataPtr;
                    byte* matchPtr;
                    uint remainingSize = decodedSize;
                    ushort flag = 0x80;
                    ushort matchOffset;
                    int matchLength;
                    bool carry;

                    while (remainingSize != 0)
                    {
                        flag += flag;
                        carry = (flag > 0xff);
                        flag &= 0x00ff;

                        if (flag == 0)
                        {
                            flag = reader.ReadByte();
                            flag += flag;
                            if (carry)
                                ++flag;
                            carry = (flag > 0xff);
                            flag &= 0x00ff;
                        }

                        if (!carry)
                        {
                            matchOffset = reader.ReadByte();
                            matchLength = (matchOffset & 0x000f) + 3;
                            matchOffset <<= 4;
                            matchOffset &= 0xff00;
                            matchOffset |= reader.ReadByte();
                            matchPtr = dstPtr - matchOffset;
                            while (matchLength-- != 0)
                            {
                                *(dstPtr++) = *(matchPtr++);
                                --remainingSize;
                            }
                        }
                        else
                        {
                            *(dstPtr++) = reader.ReadByte();
                            --remainingSize;
                        }
                    }
                }

                return new DataReader(decodedData);
            }
        }

        private static byte[] DecryptJHFile(DataReader reader, ushort key, int offset = 0)
        {
            byte[] data = new byte[reader.Size - reader.Position];
            int numWords = (data.Length - offset + 1) >> 1;
            ushort d0 = key, d1;

            for (int i = 0; i < offset; ++i)
                data[i] = reader.ReadByte();

            for (int i = 0; i < numWords; ++i)
            {
                var value = (reader.Position == reader.Size - 1) ? (ushort)(reader.ReadByte() << 8) : reader.ReadWord();
                value ^= d0;
                WriteWord(data, offset + i * 2, value);
                d1 = d0;
                d0 <<= 4;
                d0 = (ushort)((d0 + d1 + 87) % (ushort.MaxValue + 1));
            }

            return data;
        }

        private static void WriteWord(byte[] data, int offset, ushort word)
        {
            data[offset] = (byte)(word >> 8);

            if (offset < data.Length - 1)
                data[offset + 1] = (byte)(word & 0xff);
        }
    }
}
