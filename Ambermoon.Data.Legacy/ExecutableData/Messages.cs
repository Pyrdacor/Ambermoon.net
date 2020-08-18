using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// All kind of game messages.
    /// 
    /// They follow after the <see cref="WorldNames"/>.
    /// 
    /// They are stored as sections. A section can contain
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
    /// to insert/replace? The resulting string is produces by reading the
    /// partial strings at all offsets and concatenate them. Between each
    /// of the partial strings there will be a value provide by the game
    /// at runtime. So you can add C# format placeholders like {0} there.
    /// 
    /// I am not sure yet if I understand this encoding correctly
    /// but it works for now to parse all messages.
    /// </summary>
    public class Messages
    {
        public List<string> Entries { get; } = new List<string>();
        // Found 300 words in-between the messages. Maybe something with dictionary entries? Words to say?
        public List<uint> UnknownIndices { get; }
        const int NumFirstMessages = 42;
        const int NumSecondMessages = 295;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the message sections just behind the
        /// insert disk messages.
        /// 
        /// It will be behind all the message sections after this.
        /// </summary>
        internal Messages(IDataReader dataReader)
        {
            while (Entries.Count < NumFirstMessages)
                ReadText(dataReader);

            --dataReader.Position;
            dataReader.AlignToWord();

            int numIndices = (int)dataReader.ReadDword();
            UnknownIndices = new List<uint>(numIndices);

            for (int i = 0; i < numIndices; ++i)
                UnknownIndices.Add(dataReader.ReadWord());

            while (Entries.Count < NumFirstMessages + NumSecondMessages)
                ReadText(dataReader);

            dataReader.AlignToWord();
        }

        void ReadText(IDataReader dataReader)
        {
            if (dataReader.PeekWord() == 0) // offset section / split text with placeholders
            {
                dataReader.AlignToWord();

                if (dataReader.PeekWord() != 0)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid text section.");

                while (dataReader.PeekDword() == 0)
                    dataReader.Position += 4;

                string text = "";
                var offsets = new List<uint>();
                int endOffset = dataReader.Position;

                while (dataReader.PeekByte() == 0)
                {
                    offsets.Add(dataReader.ReadDword());
                    dataReader.ReadDword(); // TODO: always 0? Maybe some placeholder index reshifting?
                }

                for (int i = 0; i < offsets.Count; ++i)
                {
                    dataReader.Position = (int)offsets[i];
                    text += dataReader.ReadNullTerminatedString();

                    if (i != offsets.Count - 1) // Insert placeholder
                        text += "{" + i + "}";

                    if (dataReader.Position > endOffset)
                        endOffset = dataReader.Position;
                }

                Entries.Add(text);
                dataReader.Position = endOffset;
            }
            else // just a text
            {
                Entries.Add(dataReader.ReadNullTerminatedString());
            }            
        }
    }
}
