using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextContainerWriter : ITextContainerWriter
    {
        public void WriteTextContainer(TextContainer textContainer, IDataWriter dataWriter)
        {
            int numFormatMessageOffsets = textContainer.WorldNames.Count + textContainer.FormatMessages.Count;
            int formatMessageDataSize = textContainer.WorldNames.Sum(n => n.Length + 1);
            var formatMessages = new List<string>(textContainer.FormatMessages);
            int i;

            string CheckAndReplaceMouseClickMessage(string text)
            {
                if (!text.EndsWith(TextContainerReader.MouseClickMessage))
                    throw new AmbermoonException(ExceptionScope.Data, "Missing mouse click message in multi-line text.");

                int length = TextContainerReader.MouseClickMessage.Length;

                return text = text[..^length];
            }

            formatMessages[7] = CheckAndReplaceMouseClickMessage(formatMessages[7]);
            formatMessages[8] = CheckAndReplaceMouseClickMessage(formatMessages[8]);

            for (i = 0; i < TextContainerReader.FormatStringMergeInfos.Length; ++i)
            {
                var mergeInfo = TextContainerReader.FormatStringMergeInfos[i];
                var text = formatMessages[mergeInfo.FormatMessageIndex];
                var parts = text.Split('{', '}'); // there is a '0' where a placeholder would be
                bool placeholder = mergeInfo.FirstFormatMessageTextPartIndex != 0;
                bool first = true;

                for (int p = 0; p < mergeInfo.NumTotalParts; ++p)
                {
                    if (!placeholder)
                    {
                        if (first)
                        {
                            formatMessages[mergeInfo.FormatMessageIndex] = parts[p];
                            first = false;
                        }
                        else
                        {
                            formatMessages.Add(parts[p]);
                        }
                    }

                    placeholder = !placeholder;
                }
            }

            formatMessageDataSize += formatMessages.Sum(m => m.Length + 1); // +1 for terminating 0, or 0xff for multi-line strings
            int formatMessageDataSizeInLongs = formatMessageDataSize + 3;
            formatMessageDataSizeInLongs >>= 2;

            dataWriter.Write((ushort)formatMessageDataSizeInLongs);
            dataWriter.Write((ushort)numFormatMessageOffsets);

            int offset = 0;

            for (i = 0; i < 3; ++i)
            {
                dataWriter.Write((ushort)offset);
                offset += textContainer.WorldNames[i].Length + 1;
            }

            for (; i < numFormatMessageOffsets; ++i)
            {
                dataWriter.Write((ushort)offset);
                offset += formatMessages[i - 3].Length + 1;
            }

            foreach (var worldName in textContainer.WorldNames)
                dataWriter.WriteNullTerminated(worldName);

            i = 0;

            foreach (var formatMessage in formatMessages)
            {
                dataWriter.WriteNullTerminated(formatMessage);

                if (i == 7 || i == 8)
                    dataWriter.Write((byte)0xff);
            }

            while (formatMessageDataSize++ % 4 != 0)
                dataWriter.Write((byte)0);

            var placeholderRegex = new Regex(@"\{[0-9]+:([0-9]+)\}", RegexOptions.Compiled);

            void WriteTextSection(List<string> texts, bool allowPlaceholders)
            {
                static string CreatePlaceholder(int length)
                {
                    if (length < 1 || length > 10)
                        throw new AmbermoonException(ExceptionScope.Data, "Placeholder length out of range (allowed is 1 to 10).");

                    string placeholder = "";

                    for (int i = 0; i < length; ++i)
                        placeholder += (char)('0' + i);

                    return placeholder;
                }

                dataWriter.Write((ushort)texts.Count);

                List<string> processedTexts = new List<string>(texts.Count);

                foreach (var text in texts)
                {
                    string processedText = text;

                    if (allowPlaceholders)
                    {
                        while (true)
                        {
                            var match = placeholderRegex.Match(processedText);

                            if (!match.Success)
                                break;

                            processedText = processedText.Remove(match.Index, match.Length);
                            processedText = processedText.Insert(match.Index, CreatePlaceholder(match.Groups[1].Length));
                            dataWriter.Write((byte)0xff);
                            dataWriter.Write((byte)match.Index);
                        }
                    }

                    processedTexts.Add(processedText);
                    dataWriter.Write((ushort)(processedText.Length + 1));
                }

                int offset = dataWriter.Position;

                foreach (var text in processedTexts)
                    dataWriter.WriteNullTerminated(text);

                int size = dataWriter.Position - offset;

                while (size++ % 4 != 0)
                    dataWriter.Write((byte)0);
            }

            void WriteSimpleTextSection(List<string> texts, int expectedAmount)
            {
                if (expectedAmount != texts.Count)
                    throw new AmbermoonException(ExceptionScope.Data, $"Invalid number of text: {texts.Count}, expected: {expectedAmount}.");

                int size = texts.Sum(t => t.Length + 1);
                int sizeInLongs = (size + 3) >> 2;

                dataWriter.Write((ushort)sizeInLongs);

                foreach (var text in texts)
                    dataWriter.WriteNullTerminated(text);

                while (size++ % 4 != 0)
                    dataWriter.Write((byte)0);
            }

            WriteTextSection(textContainer.Messages, false);
            WriteSimpleTextSection(textContainer.AutomapTypeNames, 17);
            WriteSimpleTextSection(textContainer.OptionNames, 5);
            WriteSimpleTextSection(textContainer.MusicNames, 32);
            WriteSimpleTextSection(textContainer.SpellClassNames, 7);
            WriteSimpleTextSection(textContainer.SpellNames, 210);
            WriteSimpleTextSection(textContainer.LanguageNames, 8);
            WriteSimpleTextSection(textContainer.ClassNames, 9);
            WriteSimpleTextSection(textContainer.RaceNames, 15);
            WriteSimpleTextSection(textContainer.SkillNames, 10);
            WriteSimpleTextSection(textContainer.AttributeNames, 9);
            WriteSimpleTextSection(textContainer.SkillShortNames, 10);
            WriteSimpleTextSection(textContainer.AttributeShortNames, 9);
            WriteSimpleTextSection(textContainer.ItemTypeNames, 20);
            WriteSimpleTextSection(textContainer.ConditionNames, 16);
            WriteTextSection(textContainer.UITexts, true);
        }
    }
}
