using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TextContainerWriter : ITextContainerWriter
    {
        public void WriteTextContainer(TextContainer textContainer, IDataWriter dataWriter, bool withProcessedUIPlaceholders)
        {
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

            formatMessages[4] = CheckAndReplaceMouseClickMessage(formatMessages[4]);
            formatMessages[5] = CheckAndReplaceMouseClickMessage(formatMessages[5]);

            for (i = 0; i < TextContainerReader.FormatStringMergeInfos.Length; ++i)
            {
                var mergeInfo = TextContainerReader.FormatStringMergeInfos[i];
                var text = formatMessages[mergeInfo.FormatMessageIndex];
                var parts = text.Split(new char[] { '{', '}' }, System.StringSplitOptions.RemoveEmptyEntries); // there is a '0' where a placeholder would be
                bool placeholder = mergeInfo.FirstFormatMessageTextPartIndex != 0;
                bool first = true;
                int index = mergeInfo.FormatMessageIndex;

                for (int p = 0; p < mergeInfo.NumTotalParts; ++p)
                {
                    if (!placeholder)
                    {
                        if (first)
                        {
                            formatMessages[index++] = parts[p];
                            first = false;
                        }
                        else
                        {
                            formatMessages.Insert(index++, parts[p]);
                        }
                    }

                    placeholder = !placeholder;
                }
            }

            int numFormatMessageOffsets = textContainer.WorldNames.Count + formatMessages.Count;
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
                if (i == 7 || i == 8)
                {
                    dataWriter.WriteNullTerminated(formatMessage.Replace('\n', '\0').TrimEnd('\0'));
                    dataWriter.Write((byte)0xff);
                }
                else
                {
                    dataWriter.WriteNullTerminated(formatMessage);
                }

                ++i;
            }

            while (formatMessageDataSize++ % 4 != 0)
                dataWriter.Write((byte)0);

            var placeholderRegex = withProcessedUIPlaceholders
                ? new Regex(@"\{[0-9]+:([0-9]+)\}", RegexOptions.Compiled)
                : new Regex(@"0(1(2(3(4(5(6(7(89?)?)?)?)?)?)?)?)?", RegexOptions.Compiled);

            void WriteTextSection(List<string> texts, List<int> placeholderTextIndices)
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

                int countPosition = dataWriter.Position;
                dataWriter.Write((ushort)0); // reserve space for text count

                List<string> processedTexts = new List<string>(texts.Count);
                int textIndex = 0;
                int entryCount = texts.Count;

                foreach (var text in texts)
                {
                    string processedText = text;

                    if (placeholderTextIndices != null && placeholderTextIndices.Contains(textIndex))
                    {
                        if (!withProcessedUIPlaceholders)
                        {
                            var matches = placeholderRegex.Matches(processedText);

                            // There is only 1 case with two placeholders and
                            // it stores the second placeholder first.
                            for (int i = matches.Count - 1; i >= 0; --i)
                            {
                                dataWriter.Write((byte)0xff);
                                dataWriter.Write((byte)matches[i].Index);
                                ++entryCount;
                            }
                        }
                        else
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
                                ++entryCount;
                            }
                        }
                    }

                    processedTexts.Add(processedText);
                    dataWriter.Write((ushort)(processedText.Length + 1));
                    ++textIndex;
                }

                dataWriter.Replace(countPosition, (ushort)entryCount);

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

            WriteTextSection(textContainer.Messages, null);
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
            WriteSimpleTextSection(textContainer.AttributeShortNames, 8);
            WriteSimpleTextSection(textContainer.ItemTypeNames, 20);
            WriteSimpleTextSection(textContainer.ConditionNames, 16);
            WriteTextSection(textContainer.UITexts, textContainer.UITextWithPlaceholderIndices);

            int versionStringLength = (textContainer.VersionString.Length + 1 + 3) >> 2;
            int dateAndLanguageStringLength = (textContainer.DateAndLanguageString.Length + 1 + 3) >> 2;

            dataWriter.Write((byte)versionStringLength);
            dataWriter.Write((byte)dateAndLanguageStringLength);

            dataWriter.WriteNullTerminated(textContainer.VersionString);
            int padding = versionStringLength * 4 - textContainer.VersionString.Length - 1;

            for (i = 0; i < padding; ++i)
                dataWriter.Write((byte)0);

            dataWriter.WriteNullTerminated(textContainer.DateAndLanguageString);
            padding = dateAndLanguageStringLength * 4 - textContainer.DateAndLanguageString.Length - 1;

            for (i = 0; i < padding; ++i)
                dataWriter.Write((byte)0);
        }
    }
}
