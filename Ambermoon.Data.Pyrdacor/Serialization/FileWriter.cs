using Ambermoon.Data.Legacy.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public static class FileWriter
    {
        struct FileInfo
        {
            public FileFlags Flags;
            public CompressionMethod Compression;
        }

        static readonly Dictionary<FileType, FileInfo> FileTypeInfo = new Dictionary<FileType, FileInfo>
        {
            { FileType.Raw, new FileInfo { Flags = FileFlags.None } },
            { FileType.Texts, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Dictionary, new FileInfo { Flags = FileFlags.LanguageDependent | FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Palettes, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.RLE } },
            { FileType.Textures, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.TilesetGraphics, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.UIGraphics, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Graphics,  new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.RLE0 } },
            { FileType.TilesetData, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Labdata, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.CharacterData, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.MapData, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Savegames, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.OtherData, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Music, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } },
            { FileType.Video, new FileInfo { Flags = FileFlags.Compressed, Compression = CompressionMethod.LZ4 } }
        };

        static byte[] ConvertSubFile(byte[] data, FileInfo fileInfo, out bool useRaw)
        {
            byte[] compressedData = data;
            useRaw = true;

            void CheckCompression(Func<byte[], byte[]> compressor, ref bool useRaw)
            {
                var compressedData = compressor(data);

                if (compressedData.Length < data.Length)
                {
                    data = compressedData;
                    useRaw = false;
                }
            }

            if (fileInfo.Flags.HasFlag(FileFlags.Compressed))
            {
                switch (fileInfo.Compression)
                {
                    case CompressionMethod.RLE0:
                        CheckCompression(Compression.RLE0.Compress, ref useRaw);
                        break;
                    case CompressionMethod.LZ4:
                        CheckCompression(Compression.LZ4.Compress, ref useRaw);
                        break;
                    case CompressionMethod.RLE:
                        CheckCompression(Compression.RLE.Compress, ref useRaw);
                        break;
                }
            }

            return data;
        }

        public static void WriteFile(ContainerWriter writer, Dictionary<int, byte[]> filesData, FileType fileType, DataLanguage dataLanguage = DataLanguage.English)
        {
            using var stream = new MemoryStream();
            using var containerWriter = new ContainerWriter(stream, Encoding.UTF8, true, Endianness.Little);
            var fileInfo = FileTypeInfo[fileType];
            uint header = Global.BaseContainerHeader | ((uint)Global.DataVersion << 8) | ((uint)fileInfo.Flags << 4) | (uint)fileType & 0x0f;

            containerWriter.Write(header, Endianness.Big);

            if (fileInfo.Flags.HasFlag(FileFlags.LanguageDependent))
                containerWriter.Write((byte)dataLanguage);
            if (fileInfo.Flags.HasFlag(FileFlags.Compressed))
                containerWriter.Write((byte)fileInfo.Compression);

            containerWriter.Write((ushort)filesData.Count);

            foreach (var file in filesData)
            {
                var data = ConvertSubFile(file.Value, fileInfo, out bool useRaw);
                uint subFileHeader = (uint)data.Length;
                if (useRaw)
                    subFileHeader |= 0x80000000;
                containerWriter.Write(subFileHeader);
                containerWriter.Write((ushort)(file.Key & 0xffff));
                containerWriter.Write(data);
            }

            writer.Write(stream.ToArray());
        }
    }
}
