using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static Ambermoon.Data.Legacy.ExecutableData.Messages;

namespace Ambermoon.Data.Legacy.Serialization
{
    // The intro texts are grouped by sections which
    // are divided by clicks.
    //
    // 1. Destruction of the temple of brotherhood
    // 2. End of brotherhood Tarbos and peace with Moranians
    // 3. Travel to Kire's moon and rescue of the dwarves
    // 4. Valdyn leaves (no yellow teleporter stone)
    // 5. Valdyn leaves (with yellow teleporter stone)
    // 6. End texts and credits
    //
    // There are 3 sequences:
    //
    // 1. Uses 1 2 3 4 6
    // 2. Uses 1 2 3 5 6
    // 3. Uses 1 2 3 6
    //
    // In each group there are some large texts which
    // should match translations and which can help to
    // group normal texts in-between.
    //
    // For translations the last german credit texts
    // are not used where you should send feedback to
    // the Thalion office. Basically after the large
    // text "HARALD UENZELMANN" there should be 1 more
    // large text and then only normal texts to the end.
    // Everything else should be removed for translations.
    //
    // The new file Outro_texts.amb has the following format:
    //
    // word NumberOfTextGroups
    // word[n] TextCount (for each group)
    // byte[x] Null-terminated texts for all groups
    // (byte) Padding (if needed there is a padding byte)
    // word NumberOfTranslators
    // byte[x] Null-terminated translator names
    // byte[x] Null-terminated text for the click message
    // (byte) Padding (if needed there is a padding byte)

    public class OutroData : IOutroData
    {
        readonly Dictionary<OutroOption, List<OutroAction>> outroActions = new();
        readonly List<Graphic> outroPalettes = new();
        // The key is the offset inside the image hunk (it is reference by it from the data hunk).
        // The byte of the pair is the 0-based palette index (in relation to the OutroPalettes).
        readonly Dictionary<uint, KeyValuePair<Graphic, byte>> graphics = new();
        static GraphicInfo paletteGraphicInfo = new()
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };
        readonly List<string> texts = new();
        readonly Dictionary<char, Glyph> glyphs = new();
        readonly Dictionary<char, Glyph> largeGlyphs = new();

        public IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> OutroActions => outroActions.ToDictionary(a => a.Key, a => (IReadOnlyList<OutroAction>)a.Value.AsReadOnly()).AsReadOnly();
        public IReadOnlyList<Graphic> OutroPalettes => outroPalettes.AsReadOnly();
        public IReadOnlyList<Graphic> Graphics => graphics.OrderBy(g => g.Key).Select(g => g.Value.Key).ToList().AsReadOnly();
        public IReadOnlyList<string> Texts => texts.AsReadOnly();
        public IReadOnlyDictionary<uint, OutroGraphicInfo> GraphicInfos => graphics.OrderBy(g => g.Key).Select((g, i) => new { GraphicEntry = g, Index = i })
            .ToDictionary(g => g.GraphicEntry.Key, g => new OutroGraphicInfo
            {
                GraphicIndex = (uint)g.Index,
                Width = g.GraphicEntry.Value.Key.Width,
                Height = g.GraphicEntry.Value.Key.Height,
                PaletteIndex = g.GraphicEntry.Value.Value
            }
        ).AsReadOnly();
        public IReadOnlyDictionary<char, Glyph> Glyphs => glyphs.AsReadOnly();
        public IReadOnlyDictionary<char, Glyph> LargeGlyphs => largeGlyphs.AsReadOnly();

        public delegate void FontOffsetProvider(bool large, out int glyphMappingOffset,
            out int advanceValueOffset, out int glyphDataOffset);

        public OutroData(ILegacyGameData gameData)
        {
            var outroHunks = AmigaExecutable.Read(gameData.Files["Ambermoon_extro"].Files[1]);
            var codeHunks = outroHunks.Where(h => h.Type == AmigaExecutable.HunkType.Code)
                .Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var dataHunks = outroHunks
                .Where(h => h.Type == AmigaExecutable.HunkType.Data)
                .Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                GraphicFormat = GraphicFormat.Palette5Bit,
                Alpha = false,
                PaletteOffset = 0
            };
            var dataHunk = dataHunks[0];
            var imageHunk = dataHunks[1];
            var actionCache = new Dictionary<uint, List<OutroAction>>();
            var imageDataOffsets = new List<uint>();
            Graphic LoadPalette(DataReader hunk)
            {
                var paletteGraphic = new Graphic();
                graphicReader.ReadGraphic(paletteGraphic, hunk, paletteGraphicInfo);
                return paletteGraphic;
            }

            LoadFonts(codeHunks[0]);

            #region Hunk 0 - Actions and texts

            // Initial palette (all zeros)
            outroPalettes.Add(LoadPalette(dataHunk));

            // There are actually 3 outro sequence lists dependent on if Valdyn
            // is in the party and if you found the yellow teleporter sphere.
            for (int i = 0; i < 3; ++i)
            {
                var sequence = new List<OutroAction>();

                while (true)
                {
                    uint actionListOffset = dataHunk.ReadBEUInt32();

                    if (actionListOffset == 0)
                        break;

                    uint imageDataOffset = dataHunk.ReadBEUInt32();

                    if (!imageDataOffsets.Contains(imageDataOffset))
                        imageDataOffsets.Add(imageDataOffset);

                    sequence.Add(new OutroAction
                    {
                        Command = OutroCommand.ChangePicture,
                        ImageOffset = imageDataOffset
                    });

                    if (actionCache.TryGetValue(actionListOffset, out var cachedActions))
                    {
                        sequence.AddRange(cachedActions);
                    }
                    else
                    {
                        int readPosition = dataHunk.Position;
                        dataHunk.Position = (int)actionListOffset;
                        var actions = new List<OutroAction>();

                        while (true)
                        {
                            byte scrollAmount = dataHunk.ReadByte();

                            if (scrollAmount == 0xff)
                            {
                                actions.Add(new OutroAction
                                {
                                    Command = OutroCommand.WaitForClick
                                });
                                break;
                            }

                            int textDisplayX = dataHunk.ReadByte();
                            bool largeText = dataHunk.ReadByte() != 0;
                            string text = dataHunk.ReadNullTerminatedString();
                            int? textIndex = text.Length == 0 ? null : texts.Count;

                            if (text.Length != 0)
                                texts.Add(text);

                            actions.Add(new OutroAction
                            {
                                Command = OutroCommand.PrintTextAndScroll,
                                LargeText = largeText,
                                TextIndex = textIndex,
                                ScrollAmount = scrollAmount + 1,
                                TextDisplayX = textDisplayX
                            });
                        }

                        sequence.AddRange(actions);
                        actionCache.Add(actionListOffset, actions);
                        dataHunk.Position = readPosition;
                    }
                }

                outroActions.Add((OutroOption)i, sequence);
            }

            #endregion

            #region Hunk 1 - Images

            Graphic LoadGraphic(int width, int height)
            {
                graphicInfo.Width = width;
                graphicInfo.Height = height;
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, imageHunk, graphicInfo);
                return graphic;
            }

            foreach (var imageDataOffset in imageDataOffsets)
            {
                imageHunk.Position = (int)imageDataOffset;
                int width = imageHunk.ReadBEUInt16() * 16;
                int height = imageHunk.ReadBEUInt16();
                imageHunk.Position += 2; // unused word
                byte paletteIndex = (byte)outroPalettes.Count;
                outroPalettes.Add(LoadPalette(imageHunk));
                graphics.Add(imageDataOffset, KeyValuePair.Create(LoadGraphic(width, height), paletteIndex));
            }

            #endregion

            // Special handling of the new "remake-only" Extro_texts.amb
            if (gameData.Files.TryGetValue("Extro_texts.amb", out var outroTextsContainer))
            {
                var outroTextsReader = outroTextsContainer.Files[1];
                outroTextsReader.Position = 0;
                int clickGroupCount = outroTextsReader.ReadWord();
                var clickGroupSizes = new int[clickGroupCount];
                var newTextClickGroups = new List<List<string>>[clickGroupCount];

                for (int i = 0; i < clickGroupCount; ++i)
                {
                    int count = clickGroupSizes[i] = outroTextsReader.ReadWord();
                    newTextClickGroups[i] = new(count);
                }

                for (int i = 0; i < clickGroupCount; ++i)
                {
                    int groupCount = clickGroupSizes[i];
                    var groupSizes = new int[groupCount];

                    for (int g = 0; g < groupCount; ++g)
                    {
                        int count = groupSizes[g] = outroTextsReader.ReadWord();
                        newTextClickGroups[i].Add(new(count));
                    }

                    for (int g = 0; g < groupCount; ++g)
                    {
                        var newTextGroups = newTextClickGroups[i][g];
                        int count = groupSizes[g];

                        for (int t = 0; t < count; ++t)
                            newTextGroups.Add(outroTextsReader.ReadNullTerminatedString(System.Text.Encoding.UTF8));

                        newTextClickGroups[i].Add(newTextGroups);
                    }

                    if (outroTextsReader.Position % 2 == 1)
                        ++outroTextsReader.Position;
                }

                int translatorCount = outroTextsReader.ReadWord();
                var translators = new List<string>();

                for (int i = 0; i < translatorCount; ++i)
                    translators.Add(outroTextsReader.ReadNullTerminatedString(System.Text.Encoding.UTF8));

                var clickText = outroTextsReader.ReadNullTerminatedString(System.Text.Encoding.UTF8);

                if (outroTextsReader.Position % 2 == 1)
                    ++outroTextsReader.Position;

                PatchTexts(newTextClickGroups, translators, clickText, 44);
            }
        }

        void PatchTexts(List<List<string>>[] newTextGroups, List<string> translators, string clickText, int maxLineLength)
        {
            if (newTextGroups.Length != 6)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong count of outro text groups.");

            // Texts starting with an underscore are large texts.
            // Texts starting with a single dollar sign mark the name of the translator dummy.
            // Texts starting with two dollar signs mark the description of the translator.
            var processedTexts = new List<string[]>[6];

            string[] GetWords(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    return Array.Empty<string>();

                if (line.StartsWith(' '))
                {
                    int wordStartIndex = 0;

                    while (line[wordStartIndex++] == ' ')
                        ;

                    int wordEndIndex = line.IndexOf(' ', wordStartIndex + 1);

                    if (wordEndIndex == -1) // only one word
                        return new string[1] { line };

                    return Enumerable.Concat(new string[1] { line[0..wordEndIndex] }, line[(wordEndIndex + 1)..].Split(' ')).ToArray();
                }
                else
                {
                    return line.Split(' ');
                }
            }

            bool IsLarge(string text)
            {
                if (text.Length == 0) return false;
                if (text[0] == '_') return true;
                return text.StartsWith("$_");
            }

            // This will re-arrange text lines to fit into the max line length-
            // It may add or remove some lines but won't touch large text lines
            // (headings) or the click texts.
            #region Process text lines
            int clickGroupIndex = 0;
            foreach (var clickGroup in newTextGroups)
            {
                var processedClickGroup = new List<string[]>(clickGroup.Count);

                foreach (var group in clickGroup)
                {
                    var processedGroup = new List<string>(group.Count);

                    for (int i = 0; i < group.Count; ++i)
                    {
                        group[i] = group[i].TrimEnd();
                        var text = group[i];

                        if (text.StartsWith('$'))
                        {
                            processedGroup.Add(text);
                            continue;
                        }

                        if (IsLarge(text))
                        {
                            processedGroup.Add(text);
                            continue;
                        }

                        if (i == group.Count - 1) // can't move excess text to next line in this case
                        {
                            string remainingText = text.Trim();

                            while (remainingText.Length != 0)
                            {
                                if (remainingText.Length > maxLineLength)
                                {
                                    var words = GetWords(remainingText);

                                    if (words[0].Length > maxLineLength)
                                        throw new AmbermoonException(ExceptionScope.Data, "Outro text could not be fit in.");

                                    string fitText = words[0];

                                    for (int w = 1; w < words.Length; ++w)
                                    {
                                        if (fitText.Length + 1 + words[w].Length > maxLineLength)
                                            break;

                                        fitText += " " + words[w];
                                    }

                                    remainingText = remainingText[fitText.Length..].TrimStart();
                                    processedGroup.Add(fitText);
                                }
                                else
                                {
                                    processedGroup.Add(remainingText);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bool moveExceedingWordsToNextLine = true;

                            // Check if words of the next line fit into the current line
                            var nextWords = GetWords(group[i + 1].TrimEnd());

                            if (nextWords.Length != 0)
                            {
                                var words = GetWords(text);
                                int lineLength = text.Length;
                                int consumedNextWords = 0;

                                // + 1 as we need a space character in between
                                while (consumedNextWords < nextWords.Length && lineLength + 1 + nextWords[consumedNextWords].Length <= maxLineLength)
                                {
                                    // Add the word to the current line
                                    group[i] += " " + nextWords[consumedNextWords++];
                                    lineLength = group[i].Length;
                                }

                                // Remove moved words from next line
                                if (consumedNextWords != 0)
                                {
                                    group[i + 1] = string.Join(' ', nextWords.Skip(consumedNextWords));

                                    // Note: in this case it does not make sense to move words of
                                    // the current line to the next line.
                                    moveExceedingWordsToNextLine = false;
                                }
                            }

                            if (moveExceedingWordsToNextLine)
                            {
                                var words = new List<string>(GetWords(text));

                                while (group[i].Length > maxLineLength)
                                {
                                    if (words.Count == 1)
                                        throw new AmbermoonException(ExceptionScope.Data, "Outro text could not be fit in.");

                                    group[i + 1] = words[^1] + " " + group[i + 1];
                                    words.RemoveAt(words.Count - 1);
                                    group[i] = string.Join(' ', words);
                                }
                            }

                            if (group[i].Trim().Length != 0)
                                processedGroup.Add(group[i]);
                        }
                    }

                    processedClickGroup.Add(processedGroup.ToArray());
                }

                processedTexts[clickGroupIndex++] = processedClickGroup;
            }
            #endregion

            var baseTextGroups = GroupActions(outroActions.SelectMany(action => action.Value.Select(a => KeyValuePair.Create(action.Key, a))));
            var groups = new ClickGroup[6]
            {
                baseTextGroups[OutroOption.ValdynInPartyNoYellowSphere][0],
                baseTextGroups[OutroOption.ValdynInPartyNoYellowSphere][1],
                baseTextGroups[OutroOption.ValdynInPartyNoYellowSphere][2],
                baseTextGroups[OutroOption.ValdynInPartyNoYellowSphere][3],
                baseTextGroups[OutroOption.ValdynInPartyWithYellowSphere][3],
                baseTextGroups[OutroOption.ValdynInPartyNoYellowSphere][4]
            };
            var newActionLists = new List<OutroAction>[6] { new(), new(), new(), new(), new(), new() };
            texts.Clear();

            static string ProcessText(string text)
            {
                return (text[0] == '_' ? text[1..] : text).Trim();
            }

            // If in the source there was only 1 line of text but
            // we have to split it into 2 or more lines due to its
            // length in the translation, we need to know how much
            // the first text should scroll.
            const int defaultSmallTextScroll = 13;

            for (int i = 0; i < 6; ++i)
            {
                var clickGroup = groups[i];
                var newTexts = processedTexts[i];
                int groupIndex = 0;
                var newActions = newActionLists[i];

                foreach (var textGroup in clickGroup.Groups)
                {
                    if (textGroup.ChangePictureAction != null)
                        newActions.Add(textGroup.ChangePictureAction.Value);

                    var texts = newTexts[groupIndex++];
                    int t;

                    for (t = 0; t < textGroup.TextActions.Count; ++t)
                    {
                        if (t == texts.Length)
                        {
                            // The translation needs fewer text lines.
                            // We need to use the last scroll amount.
                            var lastTextAction = textGroup.TextActions.Skip(t).LastOrDefault(a => a.Command == OutroCommand.PrintTextAndScroll && a.TextIndex != null);
                            if (lastTextAction.TextIndex != null)
                                newActions[^1] = newActions[^1] with { ScrollAmount = lastTextAction.ScrollAmount };
                            break;
                        }

                        var textAction = textGroup.TextActions[t];

                        if (textAction.Command == OutroCommand.WaitForClick ||
                            textAction.TextIndex == null)
                            break;

                        bool largeText = texts[t][0] == '_';

                        if (largeText && !textAction.LargeText)
                            break;

                        textAction = textAction with { TextIndex = this.texts.Count };
                        if (translators.Count > 0 && texts[t].StartsWith("$") && !texts[t].StartsWith("$$"))
                        {
                            this.texts.Add(ProcessText(translators[0]));
                            newActions.Add(textAction);
                        }
                        else if (translators.Count > 1 && texts[t].StartsWith("$$"))
                        {
                            this.texts.Add(ProcessText(texts[t]));
                            newActions.Add(textAction);

                            for (int tr = 1; tr < translators.Count; ++tr)
                            {
                                var translatorTextAction = newActions[^2] with { TextIndex = this.texts.Count };
                                var translatorDescAction = newActions[^1];
                                this.texts.Add(ProcessText(translators[tr]));
                                newActions.Add(translatorTextAction);
                                newActions.Add(translatorDescAction);
                            }
                        }
                        else
                        {
                            this.texts.Add(ProcessText(texts[t]));
                            newActions.Add(textAction);
                        }

                        if (t != 0 && largeText)
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid text patch data.");
                    }

                    int preT = t;

                    if (t < texts.Length)
                    {
                        var lastAction = newActions[^1];

                        // More texts but not enough actions.
                        if (newActions.Count(a => a.Command == OutroCommand.PrintTextAndScroll) > 1 && !newActions[^2].LargeText)
                        {
                            // At least two small text lines
                            var secondLastAction = newActions[^2];
                            int lastScrollAmount = lastAction.ScrollAmount;
                            newActions[^1] = lastAction with { ScrollAmount = secondLastAction.ScrollAmount };
                            OutroAction textAction;

                            while (t < texts.Length - 1)
                            {
                                textAction = secondLastAction with { TextIndex = this.texts.Count };
                                this.texts.Add(texts[t++]); // no need for ProcessText as the text is always small
                                newActions.Add(textAction);
                            }

                            textAction = secondLastAction with { TextIndex = this.texts.Count, ScrollAmount = lastScrollAmount };
                            this.texts.Add(texts[t++]); // no need for ProcessText as the text is always small
                            newActions.Add(textAction);
                        }
                        else
                        {
                            // Single small text line
                            int lastScrollAmount = lastAction.ScrollAmount;
                            lastAction = newActions[^1] = lastAction with { ScrollAmount = defaultSmallTextScroll };
                            OutroAction textAction;

                            while (t < texts.Length - 1)
                            {
                                textAction = lastAction with { TextIndex = this.texts.Count };
                                this.texts.Add(texts[t++]); // no need for ProcessText as the text is always small
                                newActions.Add(textAction);
                            }

                            textAction = lastAction with { TextIndex = this.texts.Count, ScrollAmount = lastScrollAmount };
                            this.texts.Add(texts[t++]); // no need for ProcessText as the text is always small
                            newActions.Add(textAction);
                        }
                    }

                    if (preT < textGroup.TextActions.Count)
                    {
                        var emptyTextActions = textGroup.TextActions.Skip(preT).Where(x => x.TextIndex == null);
                        
                        if (emptyTextActions.Any())
                        {
                            if (emptyTextActions.Count() != 1)
                                throw new AmbermoonException(ExceptionScope.Data, "Invalid text patch data.");

                            newActions.Add(emptyTextActions.First());
                        }
                        else if (textGroup.TextActions.Any(a => a.Command == OutroCommand.PrintTextAndScroll && a.TextIndex == null))
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid text patch data.");
                    }
                    else if (textGroup.TextActions.Any(a => a.Command == OutroCommand.PrintTextAndScroll && a.TextIndex == null))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid text patch data.");
                }

                newActions.Add(new OutroAction()
                {
                    Command = OutroCommand.WaitForClick
                });
            }

            var groupMappings = new List<int>[3]
            {
                new() { 0, 1, 2, 3, 5 },
                new() { 0, 1, 2, 4, 5 },
                new() { 0, 1, 2, 5 },
            };

            // Re-assign the new action lists
            for (int i = 0; i < 3; ++i)
            {
                outroActions[(OutroOption)i] = groupMappings[i].SelectMany(index => newActionLists[index]).ToList();
            }
        }

        struct ClickGroup
        {
            public struct Group
            {
                public OutroAction? ChangePictureAction;
                public bool Large;
                public List<OutroAction> TextActions;
            }

            public List<Group> Groups;
        }

        Dictionary<OutroOption, List<ClickGroup>> GroupActions(IEnumerable<KeyValuePair<OutroOption, OutroAction>> input)
        {
            var inputList = new List<KeyValuePair<OutroOption, OutroAction>>(input);
            var firstTextItem = inputList.First(item => item.Value.TextIndex != null);
            int firstTextItemIndex = inputList.IndexOf(firstTextItem);
            var clickGroups = new Dictionary<OutroOption, List<ClickGroup>>();
            var currentGroups = new List<ClickGroup.Group>();
            var currentGroup = new ClickGroup.Group()
            {
                TextActions = new List<OutroAction>()
                {
                    firstTextItem.Value,
                },
                Large = firstTextItem.Value.LargeText,
                ChangePictureAction = inputList[0].Value.Command == OutroCommand.ChangePicture ? inputList[0].Value : null,
            };

            void FinishCurrentGroup()
            {
                // If the group only consists of a single empty scroll action
                // we just add it to the last group instead.
                if (currentGroup.TextActions.Count == 1 &&
                    currentGroup.TextActions[0].Command == OutroCommand.PrintTextAndScroll &&
                    currentGroup.TextActions[0].TextIndex == null)
                {
                    currentGroups[^1].TextActions.Add(currentGroup.TextActions[0]);
                }
                else
                {
                    currentGroups.Add(currentGroup);
                }
                currentGroup = new()
                {
                    TextActions = new()
                };
            }

            void FinishCurrentClickGroup(OutroOption option)
            {
                FinishCurrentGroup();
                var clickGroup = new ClickGroup() { Groups = new(currentGroups) };
                if (!clickGroups.TryGetValue(option, out var optionClickGroups))
                    clickGroups.Add(option, new() { clickGroup });
                else
                    optionClickGroups.Add(clickGroup);
                currentGroups.Clear();
            }

            foreach (var item in inputList.Skip(firstTextItemIndex + 1))
            {
                if (item.Value.Command == OutroCommand.WaitForClick)
                {
                    currentGroup.TextActions.Add(item.Value);
                    FinishCurrentClickGroup(item.Key);
                    continue;
                }

                if (item.Value.Command == OutroCommand.ChangePicture)
                {
                    currentGroup.ChangePictureAction = item.Value;
                    continue;
                }

                if (item.Value.LargeText)
                {
                    if (currentGroup.TextActions.Count > 0)
                        FinishCurrentGroup();
                    currentGroup.Large = true;
                }
                else if (currentGroup.TextActions.Count > 0)
                {
                    var lastAction = currentGroup.TextActions[^1];

                    if (lastAction.Command == OutroCommand.PrintTextAndScroll && !lastAction.LargeText)
                    {
                        // When text indentation changes or the previous text is an empty scroll
                        // action, we will create a new paragraph group. Also if the last scroll
                        // amount was greater than the current one.
                        if (item.Value.TextIndex != null && (lastAction.TextIndex == null || lastAction.TextDisplayX != item.Value.TextDisplayX || lastAction.ScrollAmount > item.Value.ScrollAmount))
                        {
                            FinishCurrentGroup();
                        }

                        // At the end of a paragraph there is a different scroll offset but this is
                        // provide by the last line of the paragraph so this line must be included
                        // in the current group. So in this case first add the action and then finish
                        // the group. We only do this if the scroll amount gets bigger as this indicates
                        // the paragraph end. If it gets smaller it was a single line of text in the
                        // paragraph which is handled above.
                        else if (item.Value.TextIndex != null && lastAction.ScrollAmount < item.Value.ScrollAmount)
                        {
                            currentGroup.TextActions.Add(item.Value);
                            FinishCurrentGroup();
                            continue;
                        }
                    }
                }

                currentGroup.TextActions.Add(item.Value);
            }

            if (currentGroup.TextActions.Count > 0)
                FinishCurrentClickGroup(input.Last().Key);

            return clickGroups;
        }

        static readonly byte[] GlyphMappingSearchBytes = new byte[4] { 0xff, 0x42, 0xff, 0xff };
        static readonly byte[] AdvanceValuesSearchBytes = new byte[4] { 0x0b, 0x09, 0x09, 0x0a };
        static readonly byte[] LargeAdvanceValuesSearchBytes = new byte[4] { 0x15, 0x11, 0x10, 0x13 };

        static int FindByteSequence(IDataReader reader, byte[] sequence)
        {
            int matchLength = 0;

            while (reader.Position < reader.Size)
            {
                if ((reader.Size - reader.Position) + matchLength < sequence.Length)
                    return -1;

                if (reader.ReadByte() == sequence[matchLength])
                {
                    if (++matchLength == sequence.Length)
                        return reader.Position - matchLength;
                }
                else
                {
                    matchLength = 0;
                }
            }

            return -1;
        }

        unsafe void LoadFonts(IDataReader dataReader)
        {
            dataReader.Position = 0x600; // The data won't be located before that
            int glyphMappingOffset = FindByteSequence(dataReader, GlyphMappingSearchBytes);
            int advanceValueOffset = FindByteSequence(dataReader, AdvanceValuesSearchBytes);
            int largeAdvanceValueOffset = FindByteSequence(dataReader, LargeAdvanceValuesSearchBytes);

            if (glyphMappingOffset == -1 || advanceValueOffset == -1 || largeAdvanceValueOffset == -1)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid outro data");

            void LoadFont(bool large, int glyphWidth, int glyphHeight, Dictionary<char, Glyph> glyphs)
            {
                int bytesPerGlyph = glyphWidth * glyphHeight / 8;

                // Read glyph mapping
                dataReader.Position = glyphMappingOffset;
                byte[] glyphMapping = dataReader.ReadBytes(96); // 96 chars (first is space)

                // Read advance positions
                dataReader.Position = large ? largeAdvanceValueOffset : advanceValueOffset;
                byte[] advanceValues = dataReader.ReadBytes(76); // for 76 valid chars

                // Read glyph data
                int dataOffset = (large ? largeAdvanceValueOffset : advanceValueOffset) + 76;
                dataReader.Position = dataOffset;
                byte[] glyphData = dataReader.ReadBytes(76 * bytesPerGlyph); // for 76 valid chars

                for (int i = 1; i < glyphMapping.Length; ++i)
                {
                    int index = glyphMapping[i];

                    if (index == 0xff)
                        continue;

                    char ch = (char)(0x20 + i);
                    var graphic = new Graphic
                    {
                        Width = glyphWidth,
                        Height = glyphHeight,
                        IndexedGraphic = true,
                        Data = new byte[glyphWidth * glyphHeight]
                    };
                    int numBytesPerRow = (glyphWidth + 7) / 8;
                    fixed (byte* glyphPtr = &glyphData[index * bytesPerGlyph])
                    {
                        byte* ptr = glyphPtr;
                        for (int y = 0; y < glyphHeight; ++y)
                        {
                            int offset = 0;

                            for (int n = 0; n < numBytesPerRow; ++n)
                            {
                                var data = *ptr++;

                                for (int b = 0; b < 8; ++b)
                                {
                                    if ((data & (1 << (7 - b))) != 0)
                                        graphic.Data[y * numBytesPerRow * 8 + offset + b] = (byte)Enumerations.Color.White;
                                }

                                offset += 8;
                            }
                        }
                    }
                    glyphs.Add(ch, new Glyph
                    {
                        Advance = advanceValues[index],
                        Graphic = graphic
                    });
                }
            }

            // Normal font
            LoadFont(false, 16, 11, glyphs);

            // Large font
            LoadFont(true, 32, 22, largeGlyphs);
        }
    }
}
