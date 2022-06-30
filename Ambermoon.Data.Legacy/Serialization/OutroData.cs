using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public enum OutroCommand
    {
        PrintTextAndScroll,
        WaitForClick,
        ChangePicture
    }

    public enum OutroOption
    {
        /// <summary>
        /// Valdyn is inside the party when you leave the machine room.
        /// And you got the yellow sphere from the dying moranian.
        /// </summary>
        ValdynInPartyNoYellowSphere,
        /// <summary>
        /// Valdyn is inside the party when you leave the machine room.
        /// And you did not get the yellow sphere from the dying moranian.
        /// </summary>
        ValdynInPartyWithYellowSphere,
        /// <summary>
        /// Valdyn is not inside the party when you leave the machine room.
        /// </summary>
        ValdynNotInParty
    }

    public struct OutroAction
    {
        public OutroCommand Command { get; internal set; }
        public bool LargeText { get; internal set; }
        public int ScrollAmount { get; internal set; }
        public int TextDisplayX { get; internal set; }
        public int? TextIndex { get; internal set; }
        public uint? ImageOffset { get; internal set; }
    }

    public struct OutroGraphicInfo
    {
        public uint GraphicIndex { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public byte PaletteIndex { get; internal set; }
    }

    public class OutroData
    {
        readonly Dictionary<OutroOption, IReadOnlyList<OutroAction>> outroActions = new Dictionary<OutroOption, IReadOnlyList<OutroAction>>();
        readonly List<Graphic> outroPalettes = new List<Graphic>();
        // The key is the offset inside the image hunk (it is reference by it from the data hunk).
        // The byte of the pair is the 0-based palette index (in relation to the OutroPalettes).
        readonly Dictionary<uint, KeyValuePair<Graphic, byte>> graphics = new Dictionary<uint, KeyValuePair<Graphic, byte>>();
        static GraphicInfo paletteGraphicInfo = new GraphicInfo
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };
        readonly List<string> texts = new List<string>();
        readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();
        readonly Dictionary<char, Glyph> largeGlyphs = new Dictionary<char, Glyph>();

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
                                ScrollAmount = scrollAmount + 1
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
