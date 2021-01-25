using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public class FileReader : IFileReader
    {
        private class FileContainer : IFileContainer
        {
            public string Name { get; set; }
            public uint Header { get; set; }
            public FileFlags FileFlags { get; set; }
            public FileType FileType { get; set; }
            public Dictionary<int, IDataReader> Files { get; } = new Dictionary<int, IDataReader>();
        }

        DataReader ReadSubFile(byte[] data, CompressionMethod compressionMethod)
        {
            switch (compressionMethod)
            {
                case CompressionMethod.RLE0:
                    data = Compression.RLE0.Decompress(data);
                    break;
                case CompressionMethod.LZ4:
                    data = Compression.LZ4.Decompress(data);
                    break;
                case CompressionMethod.RLE:
                    data = Compression.RLE.Decompress(data);
                    break;
            }

            return new DataReader(data);
        }

        public IFileContainer ReadFile(string name, Stream stream)
        {
            using var reader = new ContainerReader(stream, Encoding.UTF8, true, Endianness.Little);

            // Note: The header is stored in big endian. The rest in little endian.
            var header = reader.ReadUInt32(Endianness.Big);

            if ((header & 0xffff0000) != Global.BaseContainerHeader)
                throw new AmbermoonException(ExceptionScope.Data, $"Not a valid file container. Header did not start with 0x{Global.BaseContainerHeader >> 16:x4}.");

            var dataVersion = (header >> 8) & 0xff;

            if (dataVersion > Global.DataVersion)
                throw new AmbermoonException(ExceptionScope.Data, $"File container data version {dataVersion} is not supported. Max supported version is {Global.DataVersion}.");

            var fileType = (FileType)(header & 0xf);
            var fileFlags = (FileFlags)((header >> 4) & 0xf);
            var container = new FileContainer
            {
                Name = name,
                Header = header,
                FileFlags = fileFlags,
                FileType = fileType
            };

            CompressionMethod compressionMethod = CompressionMethod.None;
            DataLanguage dataLanguage = DataLanguage.English;

            if (fileFlags.HasFlag(FileFlags.LanguageDependent))
                dataLanguage = (DataLanguage)reader.ReadByte();
            if (fileFlags.HasFlag(FileFlags.Compressed))
                compressionMethod = (CompressionMethod)reader.ReadByte();

            uint fileCount = reader.ReadUInt16();

            for (int i = 0; i < fileCount; ++i)
            {
                uint subFileHeader = reader.ReadUInt16();
                int size = (int)(subFileHeader & 0x7fffffff);
                bool raw = (subFileHeader & 0x80000000) != 0; // this can override container's compression
                int index = reader.ReadUInt16();
                container.Files[index] = ReadSubFile(reader.ReadBytes(size), raw ? CompressionMethod.None : compressionMethod);
            }

            return container;
        }
    }
}
