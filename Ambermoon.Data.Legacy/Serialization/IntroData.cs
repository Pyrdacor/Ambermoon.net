using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Serialization;
using static Ambermoon.Data.Legacy.Serialization.AmigaExecutable;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class IntroData : IIntroData
    {
        readonly List<Graphic> introPalettes = new();
        readonly Dictionary<IntroGraphic, Graphic> graphics = new();
        static readonly Dictionary<IntroGraphic, byte> graphicPalettes = new()
        {
            { IntroGraphic.Frame, 0 }, // unknown
            { IntroGraphic.MainMenuBackground, 6 }, // 7 will work too
            { IntroGraphic.Gemstone, 4 },
            { IntroGraphic.Illien, 4 },
            { IntroGraphic.Snakesign, 4 },
            { IntroGraphic.DestroyedGemstone, 4 },
            { IntroGraphic.DestroyedIllien, 4 },
            { IntroGraphic.DestroyedSnakesign, 4 },
            { IntroGraphic.ThalionLogo, 0 },
            { IntroGraphic.Ambermoon, 0 }, // TODO: not sure
            { IntroGraphic.SunAnimation, 3 },
            { IntroGraphic.Lyramion, 3 },
            { IntroGraphic.Morag, 3 },
            { IntroGraphic.ForestMoon, 3 },
            { IntroGraphic.Meteor, 3 },
            { IntroGraphic.MeteorSparks, 8 },
            // TODO ...
        };
        static GraphicInfo paletteGraphicInfo = new()
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };
        readonly Dictionary<IntroText, string> texts = new();
        readonly Dictionary<char, Glyph> glyphs = new();
        readonly Dictionary<char, Glyph> largeGlyphs = new();

        public IReadOnlyList<Graphic> IntroPalettes => introPalettes.AsReadOnly();
        public static IReadOnlyDictionary<IntroGraphic, byte> GraphicPalettes => graphicPalettes;
        public IReadOnlyDictionary<IntroGraphic, Graphic> Graphics => graphics;
        public IReadOnlyDictionary<IntroText, string> Texts => texts;
        public IReadOnlyDictionary<char, Glyph> Glyphs => glyphs;
        public IReadOnlyDictionary<char, Glyph> LargeGlyphs => largeGlyphs;

        // This is somewhere in the code hunk so we just define it static here.
        private static readonly byte[] GlyphMapping = new byte[96]
        {
            0xff, 0x42, 0xff, 0xff, 0xff, 0xff, 0x47, 0x4b,
            0x44, 0x45, 0xff, 0x46, 0x3e, 0x48, 0x3f, 0x43,
            0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b,
            0x3c, 0x3d, 0x40, 0xff, 0x49, 0xff, 0x4a, 0x41,
            0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
            0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x17, 0x18, 0x19, 0xff, 0xff, 0xff, 0xff, 0xff,
            0x4b, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30,
            0x31, 0x32, 0x33, 0xff, 0xff, 0xff, 0xff, 0xff
        };

        public IntroData(GameData gameData)
        {
            var introHunks = AmigaExecutable.Read(gameData.Files["Ambermoon_intro"].Files[1]);
            var introDataHunks = introHunks
                .Where(h => h.Type == AmigaExecutable.HunkType.Data).Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var graphicReader = new GraphicReader();

            #region Hunk 0 - Palettes and texts

            var hunk0 = introDataHunks[0];

            Graphic LoadPalette()
            {
                var paletteGraphic = new Graphic();
                graphicReader.ReadGraphic(paletteGraphic, hunk0, paletteGraphicInfo);
                return paletteGraphic;
            }

            // TODO: There are only 8 palettes and the other 64 bytes have some other meaning!
            for (int i = 0; i < 9; ++i)
                introPalettes.Add(LoadPalette());

            hunk0.Position += 8; // 2 byte end marker (0xffff) + 3 words (offset from the position of the word to the associated town name: Gemstone: 6, Illien: 14, Snakesign: 20)

            for (int i = 0; i < 8; ++i)
            {
                byte startByte = hunk0.PeekByte();
                if (startByte != 0x20 && (startByte < 'A' || startByte > 'Z'))
                    ++hunk0.Position; // Sometimes there is an unknown start byte
                texts.Add((IntroText)i, hunk0.ReadNullTerminatedString());
            }

            if (hunk0.ReadByte() != 4) // Should contain the amount of main menu text (= 4)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong intro data.");

            for (int i = 0; i < 4; ++i)
            {
                // The 4 main menu texts are prefixed by 2 bytes (x and y render offset).
                hunk0.Position += 2; // We skip those bytes here.
                texts.Add((IntroText)(8 + i), hunk0.ReadNullTerminatedString());
            }

            // TODO: the credits will follow

            #endregion

            #region Hunk 1 - Main menu background and town graphics

            Size[] hunk1ImageSizes = new Size[8]
            {
                new Size(96, 300), // not sure
                new Size(320, 256),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128)
            };

            for (int i = 0; i < 8; ++i)
            {
                var reader = introDataHunks[1];

                if (reader.PeekDword() == 0x494d5021) // "IMP!", may be imploded
                    reader = new DataReader(Deploder.DeplodeFimp(reader).Reverse().ToArray());

                var graphicInfo = new GraphicInfo
                {
                    Width = hunk1ImageSizes[i].Width,
                    Height = hunk1ImageSizes[i].Height,
                    GraphicFormat = GraphicFormat.Palette4Bit,
                    PaletteOffset = 0,
                    Alpha = false
                };
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, reader, graphicInfo);
                graphics.Add((IntroGraphic)i, graphic);
            }

            #endregion

            #region Hunk 2 - Unknown

            // TODO

            #endregion

            #region Hunk 3 - Intro graphics (planets, etc)

            const int hunk3ImageCount = 8;

            Size[] hunk3ImageSizes = new Size[hunk3ImageCount]
            {
                new Size(128, 82), // Thalion Logo
                new Size(272, 87), // Ambermoon
                new Size(64, 64), // Sun
                new Size(128, 128), // Lyramion
                new Size(64, 64), // Morag
                new Size(64, 64), // Forest Moon
                new Size(96, 88), // Meteor
                new Size(64, 47), // Meteor Sparks
                // TODO ...
            };
            int[] hunk3FrameCounts = new int[hunk3ImageCount]
            {
                1,
                1,
                12,
                1,
                1,
                1,
                1,
                30,
                // TODO ...
            };
            const int MeteorSparkId = 7;

            for (int i = 0; i < hunk3ImageCount; ++i)
            {
                var graphicInfo = new GraphicInfo
                {
                    Width = hunk3ImageSizes[i].Width,
                    Height = hunk3ImageSizes[i].Height,
                    GraphicFormat = GraphicFormat.Palette4Bit,
                    PaletteOffset = 0,
                    Alpha = false
                };
                Graphic graphic;
                int frames = hunk3FrameCounts[i];
                if (frames == 1)
                {
                    graphic = new Graphic();
                    graphicReader.ReadGraphic(graphic, introDataHunks[3], graphicInfo);
                }
                else
                {
                    graphic = new Graphic(frames * graphicInfo.Width, graphicInfo.Height, 0);

                    for (int f = 0; f < frames; ++f)
                    {
                        if (i == MeteorSparkId)
                        {
                            // This has a bit mask for the blitter (1 bit per pixel).
                            // But we don't need it in the remake.
                            introDataHunks[3].Position += 376; // 64x47 bits (divided by 8 for byte count)
                        }

                        var frameGraphic = new Graphic();
                        graphicReader.ReadGraphic(frameGraphic, introDataHunks[3], graphicInfo);
                        graphic.AddOverlay((uint)(f * frameGraphic.Width), 0, frameGraphic, false);
                    }
                }
                graphics.Add(IntroGraphic.ThalionLogo + i, graphic);
            }

            // TODO ...

            #endregion

            LoadFonts(new DataReader(((Hunk)introHunks[0]).Data));
        }

        static int FindByteSequence(IDataReader reader, int offset, params byte[] sequence)
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
            void LoadFont(bool large, int glyphWidth, int glyphHeight, Dictionary<char, Glyph> glyphs)
            {
                int bytesPerGlyph = glyphWidth * glyphHeight / 8;

                // The glyph width (advance) values and glyph data is inside the code
                // hunk so not at a fixed offset necessarily. So we search for the first
                // bytes which should be unique: 15 11 10 13
                // Should be safe to only start at offset 10000. In reality it is above 11000.
                int glyphWidthOffset = large
                    ? FindByteSequence(dataReader, 10000, 0x15, 0x11, 0x10, 0x13)
                    : 0; // TODO

                // Read glyph widths
                dataReader.Position = glyphWidthOffset;
                byte[] advanceValues = dataReader.ReadBytes(76); // for 76 valid chars

                // Read glyph data (follows immediately after the glyph widths)
                byte[] glyphData = dataReader.ReadBytes(76 * bytesPerGlyph); // for 76 valid chars

                for (int i = 1; i < GlyphMapping.Length; ++i)
                {
                    int index = GlyphMapping[i];

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
