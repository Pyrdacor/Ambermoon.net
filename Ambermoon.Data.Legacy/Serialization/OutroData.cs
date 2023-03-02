using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class OutroData : IOutroData
    {
        readonly Dictionary<OutroOption, IReadOnlyList<OutroAction>> outroActions = new();
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

        public IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> OutroActions => outroActions;
        public IReadOnlyList<Graphic> OutroPalettes => outroPalettes.AsReadOnly();
        public IReadOnlyList<Graphic> Graphics => graphics.OrderBy(g => g.Key).Select(g => g.Value.Key).ToList();
        public IReadOnlyList<string> Texts => texts.AsReadOnly();
        public IReadOnlyDictionary<uint, OutroGraphicInfo> GraphicInfos => graphics.OrderBy(g => g.Key).Select((g, i) => new { GraphicEntry = g, Index = i })
            .ToDictionary(g => g.GraphicEntry.Key, g => new OutroGraphicInfo
            {
                GraphicIndex = (uint)g.Index,
                Width = g.GraphicEntry.Value.Key.Width,
                Height = g.GraphicEntry.Value.Key.Height,
                PaletteIndex = g.GraphicEntry.Value.Value
            }
        );
        public IReadOnlyDictionary<char, Glyph> Glyphs => glyphs;
        public IReadOnlyDictionary<char, Glyph> LargeGlyphs => largeGlyphs;

        public delegate void FontOffsetProvider(bool large, out int glyphMappingOffset,
            out int advanceValueOffset, out int glyphDataOffset);

        public static IDataWriter PatchTexts(ILegacyGameData gameData, List<string> texts)
        {
            // TODO: Note that this is work in progress and was used to create the french outro for
            // the first time. It has some hardcode stuff for that purpose in it which should be
            // removed later. Maybe we don't need this at all later as the original outro should be
            // changed so that the texts and glyphs are moved to an external file.
            // Still I want to preserve the work if we need to recreate the outro before that.
            var outroHunks = AmigaExecutable.Read(gameData.Files["Ambermoon_extro"].Files[1]);
            var dataHunkInfo = outroHunks
                .Where(h => h.Type == AmigaExecutable.HunkType.Data)
                .First();
            var dataHunk = new DataReader(((AmigaExecutable.Hunk)dataHunkInfo).Data);
            var writer = new DataWriter();

            Dictionary<char, int>[] LoadGlyphAdvanceValues()
            {
                var codeHunkReader = new DataReader(((AmigaExecutable.Hunk)outroHunks.Where(h => h.Type == AmigaExecutable.HunkType.Code).First()).Data);
                codeHunkReader.Position = 0x600; // The data won't be located before that
                int glyphMappingOffset = FindByteSequence(codeHunkReader, GlyphMappingSearchBytes);
                int advanceValueOffset = FindByteSequence(codeHunkReader, AdvanceValuesSearchBytes);
                int largeAdvanceValueOffset = FindByteSequence(codeHunkReader, LargeAdvanceValuesSearchBytes);

                if (glyphMappingOffset == -1 || advanceValueOffset == -1 || largeAdvanceValueOffset == -1)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid outro data");

                // Read glyph mapping
                codeHunkReader.Position = glyphMappingOffset;
                byte[] glyphMapping = codeHunkReader.ReadBytes(96); // 96 chars (first is space)

                // Read advance positions
                codeHunkReader.Position = advanceValueOffset;
                byte[] advanceValues = codeHunkReader.ReadBytes(76); // for 76 valid chars

                // Read advance positions
                codeHunkReader.Position = largeAdvanceValueOffset;
                byte[] largeAdvanceValues = codeHunkReader.ReadBytes(76); // for 76 valid chars

                var normalAdvanceValueDictionary = new Dictionary<char, int>();
                var largeAdvanceValueDictionary = new Dictionary<char, int>();

                for (int i = 1; i < glyphMapping.Length; ++i)
                {
                    int index = glyphMapping[i];

                    if (index == 0xff)
                        continue;

                    char ch = (char)(0x20 + i);

                    normalAdvanceValueDictionary[ch] = advanceValues[index];
                    largeAdvanceValueDictionary[ch] = largeAdvanceValues[index];
                }

                normalAdvanceValueDictionary[' '] = 6;
                largeAdvanceValueDictionary[' '] = 10;

                return new[] { normalAdvanceValueDictionary, largeAdvanceValueDictionary };
            }

            var advanceValueDictionaries = LoadGlyphAdvanceValues();

            int GetTextLength(string text, bool large)
            {
                var dicts = advanceValueDictionaries;
                var dict = large ? dicts[1] : dicts[0];
                int width = 0;

                foreach (var ch in text)
                    width += dict[ch];

                return width;
            }

            writer.Write(dataHunk.ReadBytes(64)); // skip the palette (all zeros)

            var actionListOffsets = new List<uint>();
            var initialActionListOffsets = new List<uint>();

            void AdjustOffsets(uint minOffset, int change)
            {
                if (change == 0)
                    return;

                for (int i = 0; i < actionListOffsets.Count; ++i)
                {
                    if (initialActionListOffsets[i] >= minOffset)
                        actionListOffsets[i] = (uint)(actionListOffsets[i] + change);
                }
            }

            void FixOffsets()
            {
                int offset = 64;

                foreach (var actionListOffset in actionListOffsets)
                {
                    if (actionListOffset != 0)
                    {
                        writer.Replace(offset, actionListOffset);
                        offset += 8;
                    }
                    else
                        offset += 4;
                }
            }

            // 3 address lists
            for (int i = 0; i < 3; ++i)
            {
                // each list is terminated by a 0-long
                // everything else is an address relative to the hunk start

                while (true)
                {
                    uint actionListOffset = dataHunk.ReadBEUInt32();
                    writer.Write(actionListOffset);
                    actionListOffsets.Add(actionListOffset);

                    if (actionListOffset == 0)
                        break;

                    writer.Write(dataHunk.ReadBEUInt32()); // this is the image offset relative to the second hunk (this won't change so keep it)
                }
            }

            // Skip 9 unknown bytes
            writer.Write(dataHunk.ReadBytes(9));

            // reorder the texts first
            var reorderedTexts = new Dictionary<uint, Dictionary<int, string>>();

            int position = dataHunk.Position;
            var checkedOffsets = new HashSet<uint>();
            int textIndex = 0;

            foreach (var offset in actionListOffsets)
            {
                if (offset == 0)
                    continue;

                if (!checkedOffsets.Add(offset))
                    continue;

                reorderedTexts.Add(offset, new());
                dataHunk.Position = (int)offset;

                int index = 0;

                while (true)
                {
                    if (dataHunk.ReadByte() == 0xff)
                        break;

                    int x = dataHunk.ReadByte();
                    bool large = dataHunk.ReadByte() != 0;
                    string oldText = dataHunk.ReadNullTerminatedString().Trim();
                    string newText = texts[textIndex++].Trim();
                    bool nextNormalText = dataHunk.PeekByte() != 0xff && (dataHunk.PeekDword() & 0x0000ff00) == 0;
                    if (!large && textIndex < texts.Count && texts[textIndex] != "<CLIQUEZ>" && texts[textIndex] != "{{CLICK}}")
                    {
                        string nextText = texts[textIndex];
                        int baseX = x;
                        do
                        {
                            x = baseX + GetTextLength(newText, large);

                            if (x <= 320)
                                break;

                            if (!nextNormalText)
                                throw new Exception($"Text {textIndex} is {x - 320} pixels too large");

                            int lastSpaceIndex = newText.LastIndexOf(' ');

                            if (lastSpaceIndex == -1)
                                break;

                            if (newText.Length > lastSpaceIndex + 1 && newText[lastSpaceIndex + 1] == ':')
                            {
                                lastSpaceIndex = newText[0..lastSpaceIndex].LastIndexOf(' ');

                                if (lastSpaceIndex == -1)
                                    break;
                            }

                            if (nextText.Length == 0)
                                nextText = newText[(lastSpaceIndex + 1)..];
                            else
                            {
                                if (!nextText.StartsWith(' '))
                                    nextText = " " + nextText;
                                nextText = nextText.Insert(0, newText[lastSpaceIndex..]);
                            }
                            newText = newText[0..lastSpaceIndex];
                        } while (x > 320);
                        texts[textIndex] = nextText;
                    }
                    else if (!large)
                    {
                        x += GetTextLength(newText, large);
                        if (x > 320)
                            throw new Exception($"Text {textIndex} is {x - 320} pixels too large");
                    }
                    reorderedTexts[offset].Add(index++, newText);
                    Console.WriteLine(textIndex);
                    Console.WriteLine(oldText);
                    Console.WriteLine(newText);
                }
            }

            dataHunk.Position = position;

            initialActionListOffsets.AddRange(actionListOffsets);

            // We will process the action lists in order
            var sortedActionListOffsets = actionListOffsets.Distinct().ToList();
            sortedActionListOffsets.Sort();
            textIndex = 0;

            foreach (var offset in sortedActionListOffsets)
            {
                if (offset == 0)
                    continue;

                if (offset != dataHunk.Position)
                    throw new AmbermoonException(ExceptionScope.Data, "Unexpected action list offset.");

                int totalTextLengthChange = 0;
                int index = 0;

                while (true)
                {
                    byte scrollAmount = dataHunk.ReadByte();
                    writer.Write(scrollAmount);

                    if (scrollAmount == 0xff) // click marker
                        break;

                    ushort xAndSize = dataHunk.ReadWord();
                    string oldText = dataHunk.ReadNullTerminatedString();
                    string newText = reorderedTexts[offset][index++];

                    if (offset == 5552 && index >= 115 && newText != "<CLIQUEZ>")
                    {
                        writer.Replace(writer.Position - 1, (byte)(index == 123 ? 66 : 6));
                    }
                    else if (offset == 5552 && newText == "<CLIQUEZ>")
                    {
                        writer.Replace(writer.Position - 1, (byte)12);
                        xAndSize = 0x8600;
                    }
                    else if (newText.StartsWith("Steinwachs, pour"))
                    {
                        xAndSize = 0x0a00;
                        writer.Replace(writer.Position - 1, (byte)12);
                    }
                    else if (newText.StartsWith("musiques. Au"))
                    {
                        xAndSize = 0x0a00;
                        writer.Replace(writer.Position - 1, (byte)12);

                        int spaceIndex = newText.IndexOf(' ');

                        reorderedTexts[offset][index] = newText[(spaceIndex + 1)..] + " " + reorderedTexts[offset][index];
                        newText = newText[0..spaceIndex];
                    }
                    else if (newText.Contains("niveau cache?"))
                    {
                        xAndSize = 0x0a00;
                        writer.Replace(writer.Position - 1, (byte)12);

                        int spaceIndex = newText.IndexOf("? ") + 1;

                        string nextText = newText[(spaceIndex + 1)..];
                        newText = newText[0..spaceIndex];

                        spaceIndex = reorderedTexts[offset][index].IndexOf(' ');
                        nextText += " " + reorderedTexts[offset][index][0..spaceIndex];
                        reorderedTexts[offset][index] = reorderedTexts[offset][index][(spaceIndex+1)..];

                        writer.Write(xAndSize);
                        writer.WriteNullTerminated(newText);
                        totalTextLengthChange += newText.Length - oldText.Length;

                        writer.Write(scrollAmount);
                        writer.Write(xAndSize);
                        totalTextLengthChange += 3;

                        writer.WriteNullTerminated(nextText);
                        totalTextLengthChange += nextText.Length + 1;

                        continue;
                    }

                    writer.Write(xAndSize); // X pos and large text flag

                    Console.WriteLine(textIndex);
                    Console.WriteLine(oldText);
                    Console.WriteLine(newText);
                    Console.WriteLine();
                    writer.WriteNullTerminated(newText);
                    totalTextLengthChange += newText.Length - oldText.Length;

                    if (newText.Contains("DENIS"))
                    {
                        byte smallScoll = dataHunk.ReadByte();
                        ushort smallX = dataHunk.ReadWord();

                        void AddText(bool large, string text, byte scrollAmount)
                        {
                            writer.Write(scrollAmount);
                            totalTextLengthChange += 3;

                            if (large)
                            {
                                writer.Write(xAndSize);
                            }
                            else
                            {
                                writer.Write(smallX);
                            }

                            writer.WriteNullTerminated(text);
                            totalTextLengthChange += text.Length + 1;
                        }

                        AddText(false, "(aka dlfrsilver)", 12);
                        // Denis description
                        writer.Write(smallScoll);
                        writer.Write(smallX);
                        oldText = dataHunk.ReadNullTerminatedString();
                        newText = reorderedTexts[offset][index++];
                        Console.WriteLine(textIndex);
                        Console.WriteLine(oldText);
                        Console.WriteLine(newText);
                        Console.WriteLine();
                        writer.WriteNullTerminated(newText);
                        totalTextLengthChange += newText.Length - oldText.Length;
                        // CFOU!
                        AddText(true, "BERTRAND JARDEL", scrollAmount);
                        AddText(false, "(aka CFOU!)", 12);
                        AddText(false, "Aide au ressourcage du programme,", 12);
                        AddText(false, "Correctif sur le programme,", 12);
                        AddText(false, "creation d'un ressourceur de texte", 12);
                        AddText(false, "sous whdload, concassage des blocs", 12);
                        AddText(false, "de donnees du programme dans", 12);
                        AddText(false, "le cadre de la traduction.", smallScoll);
                    }
                }

                AdjustOffsets(offset + 1, totalTextLengthChange);
            }

            FixOffsets();

            var exeWriter = new DataWriter();
            int hunkIndex = outroHunks.IndexOf(dataHunkInfo);
            while (writer.Size % 4 != 0)
                writer.Write((byte)0);
            outroHunks[hunkIndex] = new AmigaExecutable.Hunk(AmigaExecutable.HunkType.Data, dataHunkInfo.MemoryFlags, writer.ToArray());
            AmigaExecutable.Write(exeWriter, outroHunks);
            return exeWriter;
        }

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
                            int? textIndex = text.Length == 0 ? (int?)null : texts.Count;

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

                outroActions.Add((OutroOption)i, sequence.AsReadOnly());
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
