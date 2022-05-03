using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;

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

        public void ReadTextContainer(TextContainer textContainer, IDataReader dataReader)
        {
            int formatMessageDataSizeInLongs = dataReader.ReadWord();
            int numFormatMessageOffsets = dataReader.ReadWord();
            var formatMessageOffsets = new int[numFormatMessageOffsets + 1];
            int i;

            for (i = 0; i < numFormatMessageOffsets; i++)
                formatMessageOffsets[i] = dataReader.ReadWord();
            formatMessageOffsets[i] = formatMessageDataSizeInLongs * 4;

            var formatMessageData = dataReader.ReadBytes(formatMessageDataSizeInLongs * 4);

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

                if (i == 7 || i == 8)
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

                for (int p = 0; p < mergeInfo.NumTotalParts; ++p)
                {
                    if (placeholder)
                        parts[p] = "{0}";
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

            void ReadTextSection(List<string> targetList, bool supportPlaceholders)
            {
                int numberOfTexts = dataReader.ReadWord();
                int[] textLengths = new int[numberOfTexts];

                for (int i = 0; i < numberOfTexts; ++i)
                    textLengths[i] = dataReader.ReadWord();

                var placeholderOffsets = new List<int>();

                for (int i = 0; i < numberOfTexts; ++i)
                {
                    int textLength = textLengths[i];

                    if ((textLength & 0xff00) == 0xff00)
                    {
                        if (!supportPlaceholders)
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid text section data.");

                        // placeholder
                        placeholderOffsets.Add(textLength & 0xff);
                    }
                    else
                    {
                        string text = dataReader.ReadString(textLength);

                        if (text[^1] == '\0')
                            text = text[..(textLength - 1)];

                        if (supportPlaceholders)
                        {
                            text = ProcessPlaceholders(text, placeholderOffsets);
                            placeholderOffsets.Clear();
                        }

                        targetList.Add(text);
                    }
                }
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
            }

            ReadTextSection(textContainer.Messages, false);
            ReadSimpleTextSection(textContainer.AutomapTypeNames, 17);
            ReadSimpleTextSection(textContainer.OptionNames, 5);
            ReadSimpleTextSection(textContainer.MusicNames, 32);
            ReadSimpleTextSection(textContainer.SpellClassNames, 7);
            ReadSimpleTextSection(textContainer.SpellNames, 210);
            ReadSimpleTextSection(textContainer.LanguageNames, 8);
            ReadSimpleTextSection(textContainer.ClassNames, 9);
            ReadSimpleTextSection(textContainer.RaceNames, 15);
            ReadSimpleTextSection(textContainer.SkillNames, 10);
            ReadSimpleTextSection(textContainer.AttributeNames, 9);
            ReadSimpleTextSection(textContainer.SkillShortNames, 10);
            ReadSimpleTextSection(textContainer.AttributeShortNames, 9);
            ReadSimpleTextSection(textContainer.ItemTypeNames, 20);
            ReadSimpleTextSection(textContainer.ConditionNames, 16);
            ReadTextSection(textContainer.UITexts, true);
        }
    }
}
