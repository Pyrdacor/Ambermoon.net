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

        public OutroData(IGameData gameData)
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

            LoadFont(codeHunks[0]);

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
                                ScrollAmount = scrollAmount
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
                int width = (imageHunk.ReadBEUInt16() & 0xfffe) * 16;
                int height = imageHunk.ReadBEUInt16();
                imageHunk.Position += 2; // unused word
                byte paletteIndex = (byte)outroPalettes.Count;
                outroPalettes.Add(LoadPalette(imageHunk));
                graphics.Add(imageDataOffset, KeyValuePair.Create(LoadGraphic(width, height), paletteIndex));
            }

            #endregion
        }

        unsafe void LoadFont(IDataReader dataReader)
        {
            // Read glyph mapping
            dataReader.Position = 0x6ea;
            byte[] glyphMapping = dataReader.ReadBytes(96); // 96 chars (first is space)

            // Read glyph mapping
            dataReader.Position = 0x76e;
            byte[] advanceValues = dataReader.ReadBytes(76); // for 76 valid chars

            // Read glyph data
            dataReader.Position = 0x7ba;
            byte[] glyphData = dataReader.ReadBytes(76 * 22); // for 76 valid chars (22 bytes per glyph, 11 rows, 16 pixel bits)

            for (int i = 1; i < glyphMapping.Length; ++i)
            {
                int index = glyphMapping[i];

                if (index == 0xff)
                    continue;

                char ch = (char)(0x20 + i);
                var graphic = new Graphic
                {
                    Width = 16,
                    Height = 11,
                    IndexedGraphic = true,
                    Data = new byte[16 * 11]
                };
                fixed (byte* glyphPtr = &glyphData[index * 22])
                {
                    byte* ptr = glyphPtr;
                    for (int y = 0; y < 11; ++y)
                    {
                        ushort line = (ushort)(((*ptr++) << 8) | (*ptr++));

                        for (int b = 0; b < 16; ++b)
                        {
                            if ((line & (1 << (15 - b))) != 0)
                                graphic.Data[y * 16 + b] = (byte)Enumerations.Color.White;
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
    }
}
