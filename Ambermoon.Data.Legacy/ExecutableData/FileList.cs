using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    // German 1.05: 0x7d42
    /// <summary>
    /// There is the list of files like Palettes.amb inside the second data
    /// hunk in the executables.
    /// 
    /// It starts with a word which gives the number of entries divided by 4.
    /// Or the number of entry offset longwords (32 bit) divided by 4.
    /// 
    /// Then there is another unknown word (seems to be 0).
    /// 
    /// Then follows the offset of the file entries. These are absolute offsets
    /// in relation to the hunk data. An offset of 0 means, there is no entry.
    /// Maybe it was used to build an index list with empty entries.
    /// 
    /// At each offset you can find a file entry.
    /// It starts with a byte which represent the disk number the file
    /// is part of (0x01 to 0x0A for disk A to J).
    /// 
    /// There are two types of files. Normal ones and split archives.
    /// Normal files are only located on 1 disk. There is only a single
    /// byte with the disk number and then immediately the file name.
    /// 
    /// A split archive consists of 2 or 3 files (like 1/2/3Map_data.amb).
    /// It starts with a 4 byte header where the 4 bytes contain
    /// the disk numbers (0 means not used). The file name can contain
    /// the ASCII character '0'. It is replaced by '1', '2', '3' or '4'
    /// if the associated byte number is not 0.
    /// 
    /// Example: 0x00 0x00 0x03 0x06 file0name ...
    /// Means:
    ///   - file3name
    ///   - file4name
    /// Cause 3rd and 4th byte are not 0.
    /// 
    /// In Ambermoon there are these combinations only:
    /// 
    /// Something Something Something 0x00 0filename
    /// -> 1/2/3filename
    /// 0x00 Something Something 0x00 0filename
    /// -> 2/3filename
    /// 
    /// Files may be listed twice so this has to be allowed.
    /// </summary>
    public class FileList
    {
        readonly Dictionary<string, char> entries = new Dictionary<string, char>();
        readonly List<string[]> indexedEntries = new List<string[]>();

        /// <summary>
        /// Key: Filename
        /// Value: Disk letter (A to J)
        /// </summary>
        public IReadOnlyDictionary<string, char> Entries => entries;
        /// <summary>
        /// This contains the file list entries in the order it
        /// was stored inside the file. There might be empty entries.
        /// Each entry can contain 0 - 4 filenames.
        /// </summary>
        public IReadOnlyList<string[]> IndexedEntries => indexedEntries.AsReadOnly();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the file list.
        /// 
        /// It will be behind the file list after this.
        /// </summary>
        internal FileList(IDataReader dataReader)
        {
            int numEntries = dataReader.ReadWord() * 4;
            dataReader.ReadWord(); // TODO: always 0?
            var offsets = new uint[numEntries];
            int endOffset = dataReader.Position;

            for (int i = 0; i < numEntries; ++i)
                offsets[i] = dataReader.ReadDword();

            for (int i = 0; i < numEntries; ++i)
            {
                if (offsets[i] == 0)
                {
                    indexedEntries.Add(new string[0]);
                    continue;
                }

                dataReader.Position = (int)offsets[i];

                byte diskNumber = dataReader.ReadByte();

                if (dataReader.PeekByte() >= 0x20)
                {
                    if (diskNumber == 0)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid disk number 0.");

                    string filename = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                    entries[filename] = (char)('A' + diskNumber - 1);
                    indexedEntries.Add(new string[1] { filename });
                }
                else
                {
                    --dataReader.Position;
                    var diskNumbers = dataReader.ReadBytes(4);
                    var baseFileName = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                    string[] filenames = new string[4];

                    for (int d = 0; d < 4; ++d)
                    {
                        if (diskNumbers[d] != 0)
                        {
                            string filename = baseFileName.Replace('0', (char)('1' + d));
                            entries[filename] = (char)('A' + diskNumbers[d] - 1);
                            filenames[d] = filename;
                        }
                    }

                    indexedEntries.Add(filenames);
                }

                if (dataReader.Position > endOffset)
                    endOffset = dataReader.Position;
            }

            dataReader.Position = endOffset;
            dataReader.AlignToWord();
        }
    }
}
