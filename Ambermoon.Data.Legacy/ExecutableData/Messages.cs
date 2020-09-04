using System.Collections.Generic;
using System.Text;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// All kind of game messages.
    /// 
    /// They follow after the <see cref="WorldNames"/>.
    /// 
    /// There are two text chunks. The first one can
    /// contain placeholders.
    /// 
    /// First chunk:
    /// ============
    /// 
    /// The messages are stored as sections. A section can contain
    /// just null-terminated texts after each other or a
    /// offset section. These sections are used for split
    /// texts that are filled with values at runtime.
    /// 
    /// An offset section starts with a word-aligned 0-longword which
    /// must be skipped then. I also found some additional 0-longwords.
    /// They should be skipped as well.
    /// 
    /// Each offset entry contains 8 bytes. First 4 bytes are the absolute
    /// offset to the text string inside the data hunk. The last 4 bytes
    /// seem to be always 0. Maybe they can adjust the index of the value
    /// to insert/replace? The resulting string is produced by reading the
    /// partial strings at all offsets and concatenate them. Between each
    /// of the partial strings there will be a value provide by the game
    /// at runtime. So you can add C# format placeholders like {0} there.
    /// 
    /// I am not sure yet if I understand this encoding correctly
    /// but it works for now to parse all messages.
    /// 
    /// Second chunk:
    /// =============
    /// 
    /// The second chunk starts with the size of entries (should be 300)
    /// as a dword. Then this amount of words follow which give the
    /// lengths of each entry.
    /// 
    /// Then the entries follow which are plain texts. They are null-
    /// terminated in most cases but not always so use the length to
    /// read them and trim the terminating nulls.
    /// </summary>
    public class Messages
    {
        readonly List<string> entries = new List<string>();
        public IReadOnlyList<string> Entries => entries.AsReadOnly();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the message sections just behind the
        /// insert disk messages.
        /// 
        /// It will be behind all the message sections after this.
        /// </summary>
        internal Messages(IDataReader dataReader)
        {
            while (ReadText(dataReader))
                ;

            --dataReader.Position;
            dataReader.AlignToWord();

            int numTextEntries = (int)dataReader.ReadDword();
            var textEntryLengths = new List<uint>(numTextEntries);

            for (int i = 0; i < numTextEntries; ++i)
                textEntryLengths.Add(dataReader.ReadWord());

            for (int i = 0; i < numTextEntries; ++i)
                entries.Add(dataReader.ReadString((int)textEntryLengths[i], AmigaExecutable.Encoding).TrimEnd('\0'));

            dataReader.AlignToWord();

            if (dataReader.PeekWord() == 0)
                dataReader.Position += 2;
        }

        bool ReadText(IDataReader dataReader)
        {
            // The next section starts with an amount of 300 as a dword.
            // If we find it we stop reading by returning false. For safety
            // we will check if the value is lower than 0x1000 as the offsets
            // here will be over 0x8000.
            var next = dataReader.PeekDword();
            var nextWord = next >> 8;

            if (nextWord > 0x100 && nextWord < 0x1000)
                return false;

            nextWord >>= 8;

            if (nextWord > 0x100 && nextWord < 0x1000)
                return false;

            if (dataReader.PeekWord() == 0) // offset section / split text with placeholders
            {
                dataReader.AlignToWord();

                if (dataReader.PeekWord() != 0)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid text section.");

                while (dataReader.PeekDword() == 0)
                    dataReader.Position += 4;

                if (dataReader.PeekByte() != 0)
                    dataReader.Position -= 2;

                string text = "";
                var offsets = new List<uint>();
                int endOffset = dataReader.Position;
                uint firstOffset = uint.MaxValue;

                while (dataReader.PeekByte() == 0 && dataReader.Position < firstOffset)
                {
                    var offset = dataReader.ReadDword();

                    if (offset != 0)
                    {
                        if (offset < firstOffset)
                            firstOffset = offset;

                        offsets.Add(offset);
                    }
                }

                for (int i = 0; i < offsets.Count; ++i)
                {
                    dataReader.Position = (int)offsets[i];
                    text += dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);

                    if (i != offsets.Count - 1) // Insert placeholder
                        text += "{" + i + "}";

                    if (dataReader.Position > endOffset)
                        endOffset = dataReader.Position;
                }

                entries.Add(text);
                dataReader.Position = endOffset;
            }
            else // just a text
            {
                entries.Add(dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            return true;
        }
    }
}
