using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextContainerReader : ITextContainerReader
    {
        public const string MouseClickMessage = "{{Click}}";

        internal struct MergeInfo
        {
            public int FormatMessageIndex;
            public int FirstFormatMessageTextPartIndex;
            public int NumTotalParts;

            public MergeInfo(int formatMessageIndex,
                int firstFormatMessageTextPartIndex,
                int numTotalParts)
            {
                FormatMessageIndex = formatMessageIndex;
                FirstFormatMessageTextPartIndex = firstFormatMessageTextPartIndex;
                NumTotalParts = numTotalParts;
            }
        }

        internal static readonly MergeInfo[] FormatStringMergeInfos = new[]
        {
            new MergeInfo(0, 0, 5),
            new MergeInfo(3, 0, 3),
            new MergeInfo(12, 1, 2),
            new MergeInfo(13, 1, 4),
            new MergeInfo(15, 1, 6),
            new MergeInfo(18, 1, 2),
            new MergeInfo(19, 1, 2),
            new MergeInfo(20, 1, 2),
            new MergeInfo(21, 0, 3),
            new MergeInfo(23, 1, 4),
            new MergeInfo(25, 1, 6),
            new MergeInfo(28, 1, 4),
            new MergeInfo(30, 1, 4),
            new MergeInfo(32, 1, 4),
            new MergeInfo(34, 0, 3),
            new MergeInfo(36, 1, 4),
            new MergeInfo(38, 1, 6),
            new MergeInfo(41, 1, 2),
        };

        public void ReadTextContainer(TextContainer textContainer, IDataReader dataReader, bool processUIPlaceholders)
        {
            int formatMessageDataSizeInLongs = dataReader.ReadWord();
            int numFormatMessageOffsets = dataReader.ReadWord();
            var formatMessageOffsets = new int[numFormatMessageOffsets + 1];
            int i;

            for (i = 0; i < numFormatMessageOffsets; i++)
                formatMessageOffsets[i] = dataReader.ReadWord();
            formatMessageOffsets[i] = formatMessageDataSizeInLongs * 4;

            var formatMessageData = dataReader.ReadBytes(formatMessageDataSizeInLongs * 4);

            // Avoid padding bytes in last text
            while (formatMessageData[formatMessageOffsets[i] - 1] == 0)
                --formatMessageOffsets[i];
            ++formatMessageOffsets[i];

            var encoding = new AmbermoonEncoding();

            void ReadText(byte[] data, int start, int end, List<string> targetList)
            {
                string text = encoding.GetString(data, start, end - start - 1);

                if (data[end - 1] != '\0')
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid format text data.");

                targetList.Add(text);
            }

            void ReadTextLines(byte[] data, int start, int end, List<string> targetList)
            {
                string text = encoding.GetString(data, start, end - start - 1);

                text = text.Replace("\0", "\n");

                if (data[end - 1] != 0xff)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid format text data.");

                text += MouseClickMessage;

                targetList.Add(text);
            }

            for (i = 0; i < 3; ++i)
            {
                int start = formatMessageOffsets[i];
                int end = formatMessageOffsets[i + 1];

                ReadText(formatMessageData, start, end, textContainer.WorldNames);
            }

            for (; i < numFormatMessageOffsets; ++i)
            {
                int start = formatMessageOffsets[i];
                int end = formatMessageOffsets[i + 1];

                if (i == 10 || i == 11)
                    ReadTextLines(formatMessageData, start, end, textContainer.FormatMessages);
                else
                    ReadText(formatMessageData, start, end, textContainer.FormatMessages);
            }

            for (i = FormatStringMergeInfos.Length - 1; i >= 0; --i)
            {
                var mergeInfo = FormatStringMergeInfos[i];
                var parts = new string[mergeInfo.NumTotalParts];
                int index = mergeInfo.FormatMessageIndex;
                bool placeholder = mergeInfo.FirstFormatMessageTextPartIndex != 0;
                int deleteCount = 0;
                int placeholderIndex = 0;

                for (int p = 0; p < mergeInfo.NumTotalParts; ++p)
                {
                    if (placeholder)
                        parts[p] = "{" + (placeholderIndex++).ToString() +"}";
                    else
                    {
                        parts[p] = textContainer.FormatMessages[index];

                        if (index++ > mergeInfo.FormatMessageIndex)
                            ++deleteCount;
                    }

                    placeholder = !placeholder;
                }

                textContainer.FormatMessages.RemoveRange(mergeInfo.FormatMessageIndex + 1, deleteCount);
                textContainer.FormatMessages[mergeInfo.FormatMessageIndex] = string.Join("", parts);
            }

            string ProcessPlaceholders(string text, List<int> placeholderOffsets)
            {
                placeholderOffsets.Sort();

                for (int i = placeholderOffsets.Count - 1; i >= 0; --i)
                {
                    int offset = placeholderOffsets[i];

                    if (text[offset] != '0') // expect 0123 etc
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid placeholder offset.");

                    char digit = '1';
                    int length = 1;

                    while (++offset < text.Length)
                    {
                        if (text[offset] != digit++)
                        {
                            // end of placeholder
                            break;
                        }

                        if (++length == 10)
                            break; // max 10 digits
                    }

                    offset = placeholderOffsets[i];
                    text = text.Remove(offset, length).Insert(offset, $"{{{i}:" + new string('0', length) + "}");
                }

                return text;
            }

            void ReadTextSection(List<string> targetList, List<int> placeholderIndicesToWrite)
            {
                var placeholderRegex = new Regex("[0-9]+(?![~])", RegexOptions.Compiled);
                int numberOfTexts = dataReader.ReadWord();
                int[] textLengths = new int[numberOfTexts];

                for (int i = 0; i < numberOfTexts; ++i)
                    textLengths[i] = dataReader.ReadWord();

                var placeholderOffsets = new List<int>();
                int textDataSize = 0;

                for (int i = 0; i < numberOfTexts; ++i)
                {
                    int textLength = textLengths[i];
                    textDataSize += textLength;

                    if ((textLength & 0xff00) == 0xff00)
                    {
                        if (placeholderIndicesToWrite == null)
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid text section data.");

                        // placeholder
                        placeholderOffsets.Add(textLength & 0xff);
                    }
                    else
                    {
                        string text = dataReader.ReadString(textLength);

                        if (text[^1] == '\0')
                            text = text[..(textLength - 1)];

                        if (placeholderIndicesToWrite != null && placeholderOffsets.Count != 0)
                        {
                            placeholderIndicesToWrite.Add(targetList.Count);

                            if (processUIPlaceholders)
                                text = ProcessPlaceholders(text, placeholderOffsets);

                            placeholderOffsets.Clear();
                        }
                        else if (processUIPlaceholders)
                        {
                            var matches = placeholderRegex.Matches(text).Where(m => m.Value.StartsWith('0')).ToList();

                            for (int m = matches.Count - 1; m >= 0; --m)
                            {
                                var match = matches[m];
                                text = text.Remove(match.Index, match.Length).Insert(match.Index, "{" + m.ToString() + ":" + new string('0', match.Length) + "}");
                            }
                        }

                        targetList.Add(text);
                    }
                }

                while (textDataSize++ % 4 != 0)
                    ++dataReader.Position;
            }

            void ReadSimpleTextSection(List<string> targetList, int amount)
            {
                int sizeInLongs = dataReader.ReadWord();
                int end = dataReader.Position + sizeInLongs * 4;
                
                for (int i = 0; i < amount; ++i)
                {
                    targetList.Add(dataReader.ReadNullTerminatedString());
                }

                if (dataReader.Position > end || end - dataReader.Position >= 4)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid simple text section or text amount.");

                dataReader.Position = end;
            }

            ReadTextSection(textContainer.Messages, null);
            ReadSimpleTextSection(textContainer.AutomapTypeNames, 17);
            ReadSimpleTextSection(textContainer.OptionNames, 5);
            ReadSimpleTextSection(textContainer.MusicNames, 32);
            ReadSimpleTextSection(textContainer.SpellClassNames, 7);
            ReadSimpleTextSection(textContainer.SpellNames, 210);
            ReadSimpleTextSection(textContainer.LanguageNames, 8);
            ReadSimpleTextSection(textContainer.ClassNames, 11);
            ReadSimpleTextSection(textContainer.RaceNames, 15);
            ReadSimpleTextSection(textContainer.SkillNames, 10);
            ReadSimpleTextSection(textContainer.AttributeNames, 9);
            ReadSimpleTextSection(textContainer.SkillShortNames, 10);
            ReadSimpleTextSection(textContainer.AttributeShortNames, 8);
            ReadSimpleTextSection(textContainer.ItemTypeNames, 20);
            ReadSimpleTextSection(textContainer.ConditionNames, 16);
            ReadTextSection(textContainer.UITexts, textContainer.UITextWithPlaceholderIndices);

            int versionStringLength = dataReader.ReadByte() * 4;
            int dateAndLanguageStringLength = dataReader.ReadByte() * 4;

            textContainer.VersionString = encoding.GetString(dataReader.ReadBytes(versionStringLength)).TrimEnd('\0');
            textContainer.DateAndLanguageString = encoding.GetString(dataReader.ReadBytes(dateAndLanguageStringLength)).TrimEnd('\0');
        }
    }
}
