using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class FileWriter
    {
        public static void WriteJH(DataWriter writer, byte[] fileData, ushort encryptKey, bool additionalLobCompression)
        {
            if (additionalLobCompression)
            {
                var lobWriter = new DataWriter();
                WriteLob(lobWriter, fileData, (uint)FileType.LOB);
                fileData = lobWriter.ToArray();
            }

            var encryptedData = Compression.JH.Crypt(fileData, encryptKey);
            uint header = (uint)FileType.JH | (uint)((ushort)((uint)FileType.JH >> 16) ^ encryptKey);

            writer.Write(header);
            writer.Write(encryptedData);
        }

        public static void WriteLob(DataWriter writer, byte[] fileData)
        {
            WriteLob(writer, fileData, (uint)FileType.LOB);
        }

        public static void WriteVol1(DataWriter writer, byte[] fileData)
        {
            WriteLob(writer, fileData, (uint)FileType.VOL1);
        }

        static void WriteLob(DataWriter writer, byte[] fileData, uint header)
        {
            var compressedData = Compression.Lob.CompressData(fileData);

            writer.Write(header);
            writer.Write((uint)fileData.Length | 0x06000000u);
            writer.Write((uint)compressedData.Length);
            writer.Write(compressedData);
        }

        public static void WriteContainer(DataWriter writer, List<byte[]> filesData, FileType fileType)
        {
            switch (fileType)
            {
                case FileType.AMNC:
                case FileType.AMNP:
                case FileType.AMBR:
                case FileType.AMPC:
                {
                    if (filesData.Count >= 0xffff) // -1 cause JH uses the 1-based index as a word
                        throw new AmbermoonException(ExceptionScope.Data, $"In a container file there can only be {0xffff-1} files at max.");

                    int fileIndex = 1;
                    var writerWithoutHeader = new DataWriter();
                    List<int> fileSizes = new List<int>();

                    foreach (var fileData in filesData)
                    {
                        int prevOffset = writerWithoutHeader.Position;

                        /*
                         * AMNC | Multiple file container (data uses [JH](JH.md) encoding). The C stands for "crypted". | 0x414d4e43 ('AMNC')
                           AMNP | Multiple file container (data uses [JH](JH.md) encoding and the files are often [LOB](LOB.md) encoded in addition). The P stands for "packed". | 0x414d4e50 ('AMNP')
                           AMBR | Multiple file container (no encryption). The R stands for "raw". | 0x414d4252 ('AMNR')
                           AMPC | Another multiple file container (only compressed, not JH encrypted) | 0x414d5043 ('AMPC')
                         */
                        if (fileType == FileType.AMNC)
                            WriteJH(writerWithoutHeader, fileData, (ushort)fileIndex++, false);
                        else if (fileType == FileType.AMBR)
                        {
                            writerWithoutHeader.Write(fileData);
                            ++fileIndex;
                        }
                        else if (fileType == FileType.AMPC)
                        {
                            WriteLob(writerWithoutHeader, fileData);
                            ++fileIndex;
                        }
                        else // AMNP
                        {
                            // this is always JH encoded and may be LOB compress if size is better
                            var lobWriter = new DataWriter();
                            WriteLob(lobWriter, fileData, (uint)FileType.LOB);
                            var compressedData = lobWriter.ToArray();
                            var data = compressedData.Length - 4 < fileData.Length ? compressedData : fileData;
                            WriteJH(writerWithoutHeader, data, (ushort)fileIndex++, false);
                        }

                        fileSizes.Add(writerWithoutHeader.Position - prevOffset);
                    }

                    writer.Write((uint)fileType);
                    writer.Write((ushort)filesData.Count);
                    fileSizes.ForEach(fileSize => writer.Write((uint)fileSize));
                    writer.Write(writerWithoutHeader.ToArray());
                }
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Data, $"File type '{fileType}' is no container format.");
            }
        }
    }
}
