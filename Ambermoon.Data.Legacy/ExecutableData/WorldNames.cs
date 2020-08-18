using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// Directly after the <see cref="FileList"/> there
    /// are the names of the 3 worlds.
    /// 
    /// It starts with 3 longwords which give the absolute
    /// offset inside the second data hunk. Each of the names
    /// is null-terminated.
    /// </summary>
    public class WorldNames
    {
        public Dictionary<World, string> Entries { get; } = new Dictionary<World, string>(3);

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the world names just behind the
        /// file list.
        /// 
        /// It will be behind the world names after this.
        /// </summary>
        internal WorldNames(IDataReader dataReader)
        {
            var offsets = new uint[3];
            int endOffset = dataReader.Position;

            for (int i = 0; i < 3; ++i)
                offsets[i] = dataReader.ReadDword();

            for (int i = 0; i < 3; ++i)
            {
                dataReader.Position = (int)offsets[i];
                Entries.Add((World)i, dataReader.ReadNullTerminatedString());

                if (dataReader.Position > endOffset)
                    endOffset = dataReader.Position;
            }

            dataReader.Position = endOffset;
            dataReader.AlignToWord();
        }
    }
}
