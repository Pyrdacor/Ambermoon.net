using Ambermoon.Data.Legacy.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using static Ambermoon.Data.Legacy.Compression.LobCompression;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class FileWriter
    {
        static byte[] AlignData(byte[] data)
        {
            if (data.Length % 2 == 0)
                return data;

            var buffer = new byte[data.Length + 1];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            return buffer;
        }

        public static void Write(DataWriter writer, IFileContainer fileContainer, LobType lobType)
        {
            var fileType = fileContainer.Header.AsFileType();

            switch (fileType)
            {
                case FileType.JH:
                    WriteJH(writer, AlignData(fileContainer.Files[1].ToArray()), (ushort)(fileContainer.Header & 0xffff), false);
                    break;
                case FileType.LOB:
                    WriteLob(writer, fileContainer.Files[1].ToArray(), lobType);
                    break;
                case FileType.VOL1:
                    WriteVol1(writer, fileContainer.Files[1].ToArray(), lobType);
                    break;
                case FileType.AMBR:
                case FileType.AMNC:
                case FileType.AMNP:
                case FileType.AMPC:
                    WriteContainer(writer, fileContainer.Files.ToDictionary(f => (uint)f.Key, f => f.Value.ToArray()), fileType, null, lobType);
                    break;
                case FileType.JHPlusAMBR:
                {
                    var ambrWriter = new DataWriter();
                    WriteContainer(ambrWriter, fileContainer.Files.ToDictionary(f => (uint)f.Key, f => f.Value.ToArray()), FileType.AMBR);
                    WriteJH(writer, AlignData(ambrWriter.ToArray()), (ushort)(fileContainer.Header & 0xffff), false);
                    break;
                }
                case FileType.JHPlusLOB:
                    WriteJH(writer, fileContainer.Files[1].ToArray(), (ushort)(fileContainer.Header & 0xffff), true, false, lobType);
                    break;
                default: // raw
                    writer.Write(fileContainer.Files[1].ToArray());
                    break;
            }
        }

        public static void WriteJH(DataWriter writer, byte[] fileData, ushort encryptKey, bool additionalLobCompression,
            bool noHeader = false, LobType lobType = LobType.Ambermoon)
        {
            if (additionalLobCompression)
            {
                var lobWriter = new DataWriter();
                WriteLob(lobWriter, fileData, (uint)FileType.LOB, lobType);
                fileData = AlignData(lobWriter.ToArray());
            }

            var encryptedData = Compression.JH.Crypt(fileData, encryptKey);

            if (!noHeader)
            {
                uint header = (uint)FileType.JH | (uint)((ushort)((uint)FileType.JH >> 16) ^ encryptKey);
                writer.Write(header);
            }

            writer.Write(encryptedData);
        }

        public static void WriteLob(DataWriter writer, byte[] fileData, LobType lobType)
        {
            WriteLob(writer, AlignData(fileData), (uint)FileType.LOB, lobType);
        }

        public static void WriteVol1(DataWriter writer, byte[] fileData, LobType lobType)
        {
            WriteLob(writer, AlignData(fileData), (uint)FileType.VOL1, lobType);
        }

        static void WriteLob(DataWriter writer, byte[] fileData, uint header, LobType lobType)
        {
            var compressedData = LobCompression.Compress(fileData, lobType);

            if (fileData.Length % 2 == 1 || compressedData.Length % 2 == 1)
                throw new AmbermoonException(ExceptionScope.Application, "Lob source or compressed data is not word-aligned.");

            writer.Write(header);
            uint lobTypeCode = (uint)lobType << 24;
            writer.Write((uint)fileData.Length | lobTypeCode);
            writer.Write((uint)compressedData.Length);
            writer.Write(compressedData);
        }

        public static void WriteContainer(DataWriter writer, FileType fileType, params byte[][] filesData)
        {
            if (filesData == null)
                WriteContainer(writer, new Dictionary<uint, byte[]>(), fileType);
            else
                WriteContainer(writer, filesData.Select((f, i) => new { f, i }).ToDictionary(f => 1 + (uint)f.i, f => f.f), fileType);
        }

        public static void WriteContainer(DataWriter writer, Dictionary<uint, byte[]> filesData, FileType fileType,
            int? minimumFileCount = null, LobType lobType = LobType.Ambermoon, bool compressDictionary = false)
        {
            switch (fileType)
            {
                case FileType.JHPlusAMBR:
                    throw new AmbermoonException(ExceptionScope.Data, $"File type '{fileType}' is no valid container format. Use the Write method instead for this file type.");
                case FileType.AMNC:
                case FileType.AMNP:
                case FileType.AMBR:
                case FileType.AMPC:
                {
                    if (filesData.Count >= 0xffff) // -1 cause JH uses the 1-based index as a word
                        throw new AmbermoonException(ExceptionScope.Data, $"In a container file there can only be {0xffff-1} files at max.");

                    if (filesData.ContainsKey(0))
                        throw new AmbermoonException(ExceptionScope.Data, "The first file must have index 1 and not 0.");

                    var writerWithoutHeader = new DataWriter();
                    int totalFileNumber = (int)filesData.Keys.Max();
                    if (minimumFileCount != null && minimumFileCount > totalFileNumber)
                        totalFileNumber = minimumFileCount.Value;
                    List<int> fileSizes = Enumerable.Repeat(0, totalFileNumber).ToList();

                    foreach (var file in filesData)
                    {
                        if (file.Value.Length == 0)
                        {
                            fileSizes[(int)file.Key - 1] = 0;
                            continue;
                        }

                        var fileData = file.Value;
                        int prevOffset = writerWithoutHeader.Position;

                        /*
                         * AMNC | Multiple file container (data uses [JH](JH.md) encoding). The C stands for "crypted". | 0x414d4e43 ('AMNC')
                           AMNP | Multiple file container (data uses [JH](JH.md) encoding and the files are often [LOB](LOB.md) encoded in addition). The P stands for "packed". | 0x414d4e50 ('AMNP')
                           AMBR | Multiple file container (no encryption). The R stands for "raw". | 0x414d4252 ('AMNR')
                           AMPC | Another multiple file container (only compressed, not JH encrypted) | 0x414d5043 ('AMPC')
                         */
                        if (fileType == FileType.AMNC)
                            WriteJH(writerWithoutHeader, fileData, (ushort)file.Key, false);
                        else if (fileType == FileType.AMBR)
                        {
                            writerWithoutHeader.Write(fileData);
                        }
                        else if (fileType == FileType.AMPC)
                        {
                            WriteLob(writerWithoutHeader, fileData, lobType);
                        }
                        else // AMNP
                        {
                            // this may be lob compressed if size is better
                            var lobWriter = new DataWriter();
                            WriteLob(lobWriter, fileData, lobType);
                            var data = lobWriter.Size - 4 < fileData.Length ? lobWriter.ToArray() : fileData;
                            bool lob = data != fileData;
                            // this is always JH encoded
                            var jhWriter = new DataWriter();
                            byte[] header = lob ? data.Take(8).ToArray() : new byte[4] { 0, 0, 0, 0 };
                            byte[] encodedData = lob ? data.Skip(8).ToArray() : data;
                            WriteJH(jhWriter, encodedData, (ushort)file.Key, false, true);
                            writerWithoutHeader.Write(header);
                            writerWithoutHeader.Write(encodedData);
                        }

                        fileSizes[(int)file.Key - 1] = writerWithoutHeader.Position - prevOffset;
                    }

                    writer.Write((uint)fileType);

                    if (compressDictionary)
                    {
                        uint largestGapSize = 0;
                        uint sectionStart = 1;
                        bool isGap = false;
                        var sections = new List<KeyValuePair<uint, uint>>();
                        var indices = new List<uint>(filesData.Keys);
                        indices.Sort();
                        uint maxIndex = indices.Max();

                        if (maxIndex > 530) // this is the limit in original code
                            throw new AmbermoonException(ExceptionScope.Application, "More than 530 files are not allowed.");

                        bool useSections = false;

                        if (minimumFileCount == null || minimumFileCount <= maxIndex)
                        {
                            // Sections are not allowed if the minimum file count
                            // exceeds the highest file index. In that case there
                            // would be empty entries at the end which can't be
                            // expressed by the section encoding.

                            for (uint i = 1; i <= maxIndex; ++i)
                            {
                                if (!filesData.TryGetValue(i, out var fileData) || fileData.Length == 0)
                                {
                                    if (i == 1)
                                        isGap = true;
                                    else if (!isGap)
                                    {
                                        sections.Add(KeyValuePair.Create(sectionStart, i - sectionStart));
                                        isGap = true;
                                        sectionStart = i;
                                    }
                                }
                                else if (isGap)
                                {
                                    uint gapSize = i - sectionStart;

                                    if (gapSize > largestGapSize)
                                        largestGapSize = gapSize;

                                    isGap = false;
                                    sectionStart = i;
                                }
                            }

                            if (maxIndex > sectionStart)
                            {
                                // isGap can't be true here, last section is always valid
                                sections.Add(KeyValuePair.Create(sectionStart, maxIndex + 1 - sectionStart));
                            }

                            // use sections?
                            useSections = sections.Count != 0 && largestGapSize > 2; // don't bother to use sections for tiny gaps
                        }

                        bool anyFileExceedsSize = fileSizes.Any(size => size > 0xffff);
                        uint mask = anyFileExceedsSize
                            ? (useSections ? 0x4000u : 0x0000u)
                            : (useSections ? 0xc000u : 0x8000u);
                        writer.Write((ushort)(mask | (uint)totalFileNumber));

                        if (useSections)
                        {
                            writer.Write((ushort)sections.Count);

                            foreach (var section in sections)
                            {
                                writer.Write((ushort)section.Key);
                                writer.Write((ushort)section.Value);
                                int index = (int)section.Key - 1;

                                for (int i = 0; i < section.Value; ++i) 
                                {
                                    if (anyFileExceedsSize)
                                        writer.Write((uint)(fileSizes[index++]));
                                    else
                                        writer.Write((ushort)(fileSizes[index++]));
                                }
                            }
                        }
                        else if (anyFileExceedsSize)
                        {
                            fileSizes.ForEach(fileSize => writer.Write((uint)fileSize));
                        }
                        else
                        {
                            fileSizes.ForEach(fileSize => writer.Write((ushort)fileSize));
                        }
                    }
                    else
                    {
                        writer.Write((ushort)totalFileNumber);
                        fileSizes.ForEach(fileSize => writer.Write((uint)fileSize));
                    }

                    writer.Write(writerWithoutHeader.ToArray());
                }
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Data, $"File type '{fileType}' is no container format.");
            }
        }
    }
}
