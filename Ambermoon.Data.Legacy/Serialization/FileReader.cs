using Ambermoon.Data.Legacy.Compression;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public IFileContainer ReadFile(string name, Stream stream)
        {
            byte[] rawData = new byte[stream.Length - stream.Position];
            stream.Read(rawData, 0, rawData.Length);

            return ReadFile(name, rawData);
        }

        public IFileContainer ReadFile(string name, byte[] rawData)
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
                    fileInfo.SingleFile = false;
                    fileInfo.NumFiles = reader.ReadWord();
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
                        FileType = FileType.AMBR,
                        NumFiles = fileContainer.Files[1].ReadWord(),
                        SingleFile = false
                    }, fileContainer.Files[1] as DataReader);
                }
            }
            else
            {
                reader.Position = 6; // here the file sizes follow (dword each)
                int offset = 6 + (fileInfo.NumFiles << 2); // 4 bytes per size entry

                for (int i = 1; i <= fileInfo.NumFiles; ++i)
                {
                    int size = (int)reader.ReadDword();
                    fileContainer.Files.Add(i, size == 0 ? new DataReader(new byte[0]) : DecodeFile(new DataReader(reader, offset, size), fileInfo.FileType, i));
                    offset += size;
                }

                reader.Position = offset;
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
                uint decodedSize = reader.PeekDword() & 0x00ffffff; // the first byte would contain the offset for the size entry (= 6)

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

                return Lob.Decompress(reader, decodedSize);
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
