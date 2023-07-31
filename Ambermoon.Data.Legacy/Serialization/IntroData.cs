using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data.Serialization;
using static Ambermoon.Data.Legacy.Serialization.AmigaExecutable;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal class IntroTwinlakeImagePart : IIntroTwinlakeImagePart
    {
        public Position Position { get; init; }

        public Graphic Graphic { get; init; }
    }

    internal readonly struct TextCommand : IIntroTextCommand
    {
        public IntroTextCommandType Type { get; private init; }
        public int[] Args { get; private init; }

        internal static bool TryParse(IDataReader dataReader, List<string> texts, out TextCommand? textCommand)
        {
            textCommand = null;
            var command = (IntroTextCommandType)dataReader.ReadByte();

            if (command > IntroTextCommandType.ActivatePaletteFading)
            {
                if ((int)command == 255)
                    return false; // End marker

                // Otherwise the data is invalid
                throw new AmbermoonException(ExceptionScope.Data, "Unsupported intro text command.");
            }

            int[] args;

            switch (command)
            {
                case IntroTextCommandType.Add:
                    args = new int[3];
                    args[0] = dataReader.ReadByte(); // X
                    args[1] = dataReader.ReadByte(); // Y
                    args[2] = texts.Count; // Text index
                    texts.Add(dataReader.ReadNullTerminatedString());
                    break;
                case IntroTextCommandType.Wait:
                    args = new int[1] { dataReader.ReadByte() }; // Ticks
                    break;
                case IntroTextCommandType.SetTextColor:
                    args = new int[1] { dataReader.ReadWord() }; // Color
                    break;
                default:
                    args = Array.Empty<int>();
                    break;
            }

            textCommand = new TextCommand
            {
                Type = command,
                Args = args
            };

            return true;
        }
    }

    public class IntroData : IIntroData
    {
        readonly List<IntroTwinlakeImagePart> twinlakeImageParts = new();
        readonly List<IIntroTextCommand> textCommands = new();
        readonly List<string> textCommandTexts = new();
        readonly List<Graphic> introPalettes = new();
        readonly Dictionary<IntroGraphic, Graphic> graphics = new();
        static readonly Dictionary<IntroGraphic, byte> graphicPalettes = new()
        {
            { IntroGraphic.Frame, 8 },
            { IntroGraphic.MainMenuBackground, 6 }, // 7 will work too
            { IntroGraphic.Gemstone, 4 },
            { IntroGraphic.Illien, 4 },
            { IntroGraphic.Snakesign, 4 },
            { IntroGraphic.DestroyedGemstone, 4 },
            { IntroGraphic.DestroyedIllien, 4 },
            { IntroGraphic.DestroyedSnakesign, 4 },
            { IntroGraphic.ThalionLogo, 0 },
            { IntroGraphic.Ambermoon, 1 },
            { IntroGraphic.SunAnimation, 3 },
            { IntroGraphic.Lyramion, 3 },
            { IntroGraphic.Morag, 3 },
            { IntroGraphic.ForestMoon, 3 },
            { IntroGraphic.Meteor, 3 },
            { IntroGraphic.MeteorSparks, 3 },
            { IntroGraphic.GlowingMeteor, 3 },
            { IntroGraphic.Twinlake, 8 },
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
        public IReadOnlyList<IIntroTwinlakeImagePart> TwinlakeImageParts => twinlakeImageParts;
        public IReadOnlyList<IIntroTextCommand> TextCommands => textCommands;
        public IReadOnlyList<string> TextCommandTexts => textCommandTexts;

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
            // It seems it is some kind of color palette as well but used in a different way (maybe a changing palette or some color replacement table which is activated over time?)
            for (int i = 0; i < 8; ++i)
                introPalettes.Add(LoadPalette());
            hunk0.Position += 64;

            // We still use 9 palettes here.
            // The intro uses the last 16 colors to fade the first 16 colors when starting the intro and vice versa.
            // As we fade with a black overlay we need the color palette when the images are fully visible (faded in).
            // At this point the 16 last colors are also the first 16 colors, so here we just copy them over.
            var palette9 = introPalettes[4].Clone();
            Array.Copy(introPalettes[4].Data, 16 * 4, palette9.Data, 0, 16 * 4);
            introPalettes.Add(palette9);

            hunk0.Position += 8; // 2 byte end marker (0xffff) + 3 words (offset from the position of the word to the associated town name: Gemstone: 6, Illien: 14, Snakesign: 20)

            for (int i = 0; i < 8; ++i)
            {
                var introText = (IntroText)i;

                if (introText >= IntroText.Gemstone && introText <= IntroText.Snakesign)
                    hunk0.Position++; // skip X byte

                texts.Add(introText, hunk0.ReadNullTerminatedString());
            }

            if (hunk0.ReadByte() != 4) // Should contain the amount of main menu text (= 4)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong intro data.");

            for (int i = 0; i < 4; ++i)
            {
                // The 4 main menu texts are prefixed by 2 bytes (x and y render offset).
                hunk0.Position += 2; // We skip those bytes here.
                texts.Add((IntroText)(8 + i), hunk0.ReadNullTerminatedString());
            }

            while (TextCommand.TryParse(hunk0, textCommandTexts, out var textCommand))
            {
                textCommands.Add(textCommand);
            }

            // TODO: here the zoom infos start with the header 00 05 which gives the amount of objects

            #endregion

            #region Hunk 1 - Main menu background and town graphics

            Size[] hunk1ImageSizes = new Size[8]
            {
                new Size(288, 200),
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

            #region Hunk 2 - Twinlake image and animation data

            // This is encoded data
            var hunk2Data = new List<byte[]>();
            var encodedReader = introDataHunks[2];
            int off = 0;
            var currentData = new List<byte>();

            while (encodedReader.Position < encodedReader.Size)
            {
                int size = encodedReader.ReadWord();
                int endOffset = off + size;

                while (off < endOffset)
                {
                    sbyte header = unchecked((sbyte)encodedReader.ReadByte());

                    if (header >= 0)
                    {
                        for (int i = 0; i < header + 1; i++)
                            currentData.Add(encodedReader.ReadByte());

                        off += header + 1;
                    }
                    else
                    {
                        byte literal = encodedReader.ReadByte();
                        int count = ~header + 1;

                        for (int i = 0; i < count; i++)
                            currentData.Add(literal);

                        off += count;
                    }
                }

                if (size != 0)
                {
                    hunk2Data.Add(currentData.ToArray());
                    currentData.Clear();

                    if (encodedReader.Position % 2 == 1)
                        encodedReader.Position++;
                }
            }

            // There are 177 iterations per data block
            // First a long is read. If 0, the iteration is skipped.
            // Otherwise the value is doubled and checked for 32-bit overflow.
            // If overflow occurs, some code is executed, otherwise skipped.
            // This continues 32 times.

            if (hunk2Data.Count != 95)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid intro hunk.");

            int blockIndex = 0;
            var graphicData = new byte[(256 * 177) / 2]; // max size and 4bpp

            foreach (var dataBlock in hunk2Data)
            {
                var blockReader = new DataReader(dataBlock);
                int left = int.MaxValue;
                int right = -1;
                int top = int.MaxValue;
                int bottom = -1;

                for (int i = 0; i < 177; i++)
                {
                    long changeHeader = blockReader.ReadDword();

                    if (changeHeader == 0)
                        continue;

                    for (int n = 0; n < 32; n++)
                    {
                        changeHeader <<= 1;

                        if ((changeHeader & 0x1_0000_0000) != 0)
                        {
                            int x = n * 8;

                            if (left > x)
                                left = x;
                            if (right < x + 8)
                                right = x + 8;
                            if (top > i)
                                top = i;
                            if (bottom < i + 1)
                                bottom = i + 1;

                            changeHeader &= 0x0_ffff_ffff;

                            // I think this changes the Twinlake picture to add
                            // the animations of fleeing people of impacts.

                            byte a = blockReader.ReadByte();
                            byte b = blockReader.ReadByte();
                            byte c = blockReader.ReadByte();
                            byte d = blockReader.ReadByte();

                            int offset = i * 32 * 4 + n;

                            graphicData[offset] ^= a;
                            offset += 32;
                            graphicData[offset] ^= b;
                            offset += 32;
                            graphicData[offset] ^= c;
                            offset += 32;
                            graphicData[offset] ^= d;

                            // Basically an exclusive OR (^) is performed on the
                            // image data at offset 0, 40, 80 and 120 (40 bytes per row, 320 bits).
                            // 4 values as there are 4 bits per pixel.
                            // For every iteration, the offset is increased by 1 to the next byte.
                            // Each byte holds 8 pixels basically.

                            // I guess the image is only 256 pixels wide, therefore only 32 instead of 40 iterations.
                            // 177 is most likely the height.

                            // The changes are performed on the screen buffer starting at 32, 7
                            // and so it goes up to 288, 184.

                            // In contrast to the Amiga version we will generate and store the graphic parts
                            // together with the locations once when loading the data and then display them later.

                            // The first frame seems to hold the base graphic. With a header of 0xffffffff you
                            // ensure that every pixel is updated in a row. The first data block uses this header
                            // all the time and as the screen is 0 beforehand, it will just print the base graphic.
                            // Later blocks often use a header of 0 to just skip large parts of the image and then
                            // for example use 0x1000. A doubling means a left shift. So the position of the first 1
                            // (from left) basically determines when something is changed. For 0x80000000 or greater
                            // the first pixel is directly changed, for 0x40000000 only the second one and so on.
                            // 0x1000 means 18 pixels are not changed and then it starts. Then the amount of 1 bits
                            // in a row keeps changing pixels. So 0xf0000000 will change the first 4 pixels in a row.
                        }
                    }
                }

                Graphic CreateGraphic()
                {
                    var graphic = new Graphic();
                    var graphicInfo = new GraphicInfo
                    {
                        Width = 256,
                        Height = 177,
                        GraphicFormat = GraphicFormat.Palette4Bit
                    };
                    var graphicDataReader = new DataReader(graphicData);
                    graphicReader.ReadGraphic(graphic, graphicDataReader, graphicInfo);
                    // Note: Color index 0 is treated as transparent. But we need this for black
                    // color here. As index 16 also has black, we just replace index 0 by 16.
                    graphic.ReplaceColor(0, 16);
                    return graphic;
                }

                if (blockIndex++ == 0)
                {
                    graphics.Add(IntroGraphic.Twinlake, CreateGraphic());
                }
                else
                {
                    twinlakeImageParts.Add(new IntroTwinlakeImagePart
                    {
                        Graphic = CreateGraphic().GetArea(left, top, right - left, bottom - top),
                        Position = new Position(left, top)
                    });
                }
            }

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
                        var frameGraphic = new Graphic();

                        if (i == MeteorSparkId)
                        {
                            // This has a bit mask plane for the blitter (1 bit per pixel).
                            // The image itself is 4bpp. So for easier handling we just load
                            // this as a 5 bpp image.                            
                            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
                            graphicReader.ReadGraphic(frameGraphic, introDataHunks[3], graphicInfo);

                            // No we have to consider the bit mask.
                            // The lowest bit is the mask. If it is 0
                            // the whole pixel should be transparent.
                            // Otherwise we use the color.
                            for (int b = 0; b < frameGraphic.Data.Length; b++)
                            {
                                // This means opaque and color index 0. This would be black but we
                                // use color index 0 for transparent pixels, so switch to color 16
                                // here which is also black.
                                if (frameGraphic.Data[b] == 1)
                                    frameGraphic.Data[b] = 16;
                                // In this case the lowest bit is not set, so use color index 0
                                // here to achieve a transparent pixel.
                                else if ((frameGraphic.Data[b] & 0x1) == 0)
                                    frameGraphic.Data[b] = 0;
                                // In all other cases we just have to shift the color value right
                                // by 1 to get rid of the mask bit and get the real color.
                                else
                                    frameGraphic.Data[b] >>= 1;
                            }
                        }
                        else
                        {
                            graphicReader.ReadGraphic(frameGraphic, introDataHunks[3], graphicInfo);
                        }
                        graphic.AddOverlay((uint)(f * frameGraphic.Width), 0, frameGraphic, false);
                    }
                }
                graphics.Add(IntroGraphic.ThalionLogo + i, graphic);
            }

            // In the original version the glowing of the meteor is performed by fading the upper 16 colors
            // of the palette to the lower 16 colors of the palette and vice versa. The meteor always uses
            // colors from the first 16 colors though.
            // As we can't do this here we add another meteor graphic where all color indices are increased
            // by 16. This way they point to the bright color in the upper 16 colors and show the meteor
            // in full glow brightness. To fade we just display the glow image above the normal meteor with
            // some level of transparency.
            var glowingMeteorGraphic = graphics[IntroGraphic.Meteor].Clone();
            for (int i = 0; i < glowingMeteorGraphic.Data.Length; ++i)
            {
                if (glowingMeteorGraphic.Data[i] != 0) // keep index 0 as it is full transparency!
                    glowingMeteorGraphic.Data[i] += 16;
            }
            graphics.Add(IntroGraphic.GlowingMeteor, glowingMeteorGraphic);

            // TODO ...

            #endregion

            LoadFonts(new DataReader(((Hunk)introHunks[0]).Data));
        }

        static int FindByteSequence(IDataReader reader, int offset, params byte[] sequence)
        {
            int matchLength = 0;
            reader.Position = offset;

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
                // Same for small glyphs. Offset is 18262 so we pick 17000 for safety.
                int glyphWidthOffset = large
                    ? FindByteSequence(dataReader, 10000, 0x15, 0x11, 0x10, 0x13)
                    : FindByteSequence(dataReader, 17000, 0x0b, 0x09, 0x09, 0x0a);

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
