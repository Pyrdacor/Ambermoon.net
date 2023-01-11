using Ambermoon.Data.Legacy.Compression;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Ambermoon.Data.Legacy.Compression.LobCompression;

namespace Ambermoon.Data.Legacy.Serialization
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
            public FileType FileType { get; set; }
            public uint Header => (uint)FileType;
            public Dictionary<int, IDataReader> Files { get; set; } = new Dictionary<int, IDataReader>();
        }

        public static IFileContainer Create(string name, FileType fileType, Dictionary<int, IDataReader> files)
        {
            return new FileContainer
            {
                Name = name,
                FileType = fileType,
                Files = files
            };
        }

        public static IFileContainer CreateRawFile(string name, byte[] fileData)
        {
            return new FileContainer
            {
                Name = name,
                FileType = FileType.None,
                Files = new Dictionary<int, IDataReader> { { 1, new DataReader(fileData) } }
            };
        }

        public static IFileContainer CreateRawContainer(string name, Dictionary<int, byte[]> fileData)
        {
            return new FileContainer
            {
                Name = name,
                FileType = FileType.AMBR,
                Files = fileData.ToDictionary(f => f.Key, f => (IDataReader)new DataReader(f.Value))
            };
        }

        public IFileContainer ReadRawFile(string name, Stream stream)
        {
            byte[] rawData = new byte[stream.Length - stream.Position];
            stream.Read(rawData, 0, rawData.Length);

            return ReadRawFile(name, rawData);
        }

        public IFileContainer ReadRawFile(string name, byte[] rawData)
        {
            return ReadFile(name, new DataReader(rawData));
        }

        public IFileContainer ReadFile(string name, IDataReader reader)
        {
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
                case FileType.AMTX:
                    fileInfo.SingleFile = false;
                    fileInfo.NumFiles = reader.ReadWord() & 0x3ff;
                    break;
                default: // raw format
                    var fileContainer = new FileContainer { Name = name };
                    fileContainer.Files.Add(1, new DataReader(reader.ToArray()));
                    return fileContainer;
            }

            reader.Position = 0;

            return ProcessFileInfo(name, fileInfo, reader);
        }

        private IFileContainer ProcessFileInfo(string name, FileInfo fileInfo, IDataReader reader)
        {
            var fileContainer = new FileContainer { Name = name, FileType = fileInfo.FileType };

            if (fileInfo.SingleFile)
            {
                fileContainer.Files.Add(1, DecodeFile(reader, fileInfo.FileType, 1));

                // There is a special case where AMBR can be inside JH.
                if (fileInfo.FileType == FileType.JH && fileContainer.Files[1].PeekDword() == (uint)FileType.AMBR)
                {
                    fileContainer.Files[1].Position += 4;
                    return ProcessFileInfo(name, new FileInfo
                    {
                        FileType = FileType.JHPlusAMBR,
                        NumFiles = fileContainer.Files[1].ReadWord() & 0x3ff,
                        SingleFile = false
                    }, fileContainer.Files[1] as DataReader);
                }

                // There is a special case where LOB can be inside JH.
                if (fileInfo.FileType == FileType.JH && fileContainer.Files[1].PeekDword() == (uint)FileType.LOB)
                {
                    fileContainer.FileType = FileType.JHPlusLOB;
                }
            }
            else
            {
                reader.Position = 4; // skip header
                ushort fileCount = reader.ReadWord();
                int type = fileCount >> 14;
                fileCount &= 0x3ff;

                if (type == 0 || type == 2)
                {
                    int entrySize = type == 2 ? 2 : 4;
                    int offset = 6 + fileCount * entrySize;
                    Func<int> fileSizeProvider = type == 2 ? () => reader.ReadWord() : () => (int)reader.ReadDword();

                    for (int i = 1; i <= fileCount; ++i)
                    {
                        int fileSize = fileSizeProvider();
                        fileContainer.Files.Add(i, fileSize == 0 ? new DataReader(Array.Empty<byte>()) : DecodeFile(new DataReader(reader, offset, fileSize), fileInfo.FileType, i));
                        offset += fileSize;
                    }

                    reader.Position = offset;
                }
                else // sections
                {
                    var fileEntries = new Dictionary<uint, KeyValuePair<int, int>>();
                    Func<int> fileSizeProvider = type == 3 ? () => reader.ReadWord() : () => (int)reader.ReadDword();
                    int sectionCount = reader.ReadWord();

                    if (sectionCount == 0)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid container section count.");

                    int offset = 0; // relative offset for now

                    for (int i = 0; i < sectionCount; ++i)
                    {
                        uint index = reader.ReadWord();
                        int sectionSize = reader.ReadWord();

                        for (int j = 0; j < sectionSize; ++j)
                        {
                            int fileSize = fileSizeProvider();
                            fileEntries.Add(index++, KeyValuePair.Create(offset, fileSize));
                            offset += fileSize;
                        }
                    }

                    offset = reader.Position;

                    for (int i = 1; i <= fileCount; ++i)
                    {
                        if (fileEntries.TryGetValue((uint)i, out var entry) && entry.Value > 0)
                            fileContainer.Files.Add(i, DecodeFile(new DataReader(reader, offset + entry.Key, entry.Value), fileInfo.FileType, i));
                        else
                            fileContainer.Files.Add(i, new DataReader(Array.Empty<byte>()));
                    }
                }
            }

            return fileContainer;
        }

        private IDataReader DecodeFile(IDataReader reader, FileType containerType, int fileNumber)
        {
            var header = reader.Size < 4 ? 0 : reader.PeekDword();
            var fileType = ((header & 0xffff0000) == (uint)FileType.JH) ? FileType.JH : (FileType)header;

            if (fileType == FileType.JH)
            {
                reader.Position += 4; // skip header
                reader = new DataReader(JH.Crypt(reader, (ushort)(((header & 0xffff0000u) >> 16) ^ (header & 0x0000ffffu))));
            }
            else if (containerType == FileType.AMNC) // AMNC archives are always encoded
            {
                reader = new DataReader(JH.Crypt(reader, (ushort)fileNumber));
            }

            header = reader.Size < 4 ? 0 : reader.PeekDword(); // Note: The header might have changed above.
            fileType = (FileType)header; // Note: No need to check for JH here as this can not happen.

            // See if it is a LOB file
            if (fileType == FileType.LOB || fileType == FileType.VOL1)
            {
                reader.Position += 4; // skip header
                uint lobHeader = reader.PeekDword();
                uint decodedSize = lobHeader & 0x00ffffff;
                LobType lobType = (LobType)(lobHeader >> 24);

                // AMNP archives are always encoded
                if (containerType == FileType.AMNP)
                {
                    reader.Position += 4; // skip decoded size
                    reader = new DataReader(JH.Crypt(reader, (ushort)fileNumber));
                    reader.Position += 4; // skip encoded size
                }
                else
                {
                    reader.Position += 8;  // skip decoded and encoded size
                }

                return Decompress(reader, decodedSize, lobType);
            }
            else
            {
                // AMNP archives are always encoded
                if (containerType == FileType.AMNP)
                {
                    // ensure and skip the header (should be FileType.None here)
                    if (reader.ReadDword() != (uint)FileType.None)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid AMNP file data.");

                    reader = new DataReader(JH.Crypt(reader, (ushort)fileNumber));
                }

                return reader;
            }            
        }
    }
}
