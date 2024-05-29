using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace AmbermoonAndroid
{
    class Text
    {
        readonly List<IAlphaSprite> renderGlyphs = new List<IAlphaSprite>();
        bool visible = false;
        readonly int totalWidth = 0;
        int baseX = 0;
        TextColor textColor = TextColor.White;
        byte alpha = 255;
        Rect clipArea;

        public Text(IRenderView renderView, Layer layer, string text, IReadOnlyDictionary<char, Glyph> glyphs,
            List<char> characters, byte displayLayer, int spaceWidth, bool upperOnly, uint textureAtlasIndexOffset,
            byte alpha = 255, Rect clipArea = null)
        {
            totalWidth = 0;
            this.alpha = alpha;
            this.clipArea = clipArea;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(layer);

            if (upperOnly)
                text = text.ToUpper();

            foreach (char ch in text)
            {
                if (ch == ' ')
                    totalWidth += spaceWidth;
                else if (glyphs.TryGetValue(ch, out var glyph))
                {
                    var sprite = renderView.SpriteFactory.CreateWithAlpha(glyph.Graphic.Width, glyph.Graphic.Height, displayLayer);
                    sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)characters.IndexOf(ch) + textureAtlasIndexOffset);
                    sprite.Alpha = alpha;
                    sprite.ClipArea = clipArea;
                    sprite.X = totalWidth;
                    sprite.Y = 0;
                    sprite.Layer = renderView.GetLayer(layer);
                    sprite.PaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);
                    sprite.Visible = false;
                    renderGlyphs.Add(sprite);
                    totalWidth += glyph.Advance;
                }
            }
        }

        public TextColor TextColor
        {
            get => textColor;
            set
            {
                if (textColor == value)
                    return;

                textColor = value;
                renderGlyphs?.ForEach(g => { if (g != null) g.MaskColor = (byte)textColor; });
            }
        }

        public byte Alpha
        {
            get => alpha;
            set
            {
                if (alpha == value)
                    return;

                alpha = value;
                renderGlyphs?.ForEach(g => { if (g != null) g.Alpha = alpha; });
            }
        }

        public Rect ClipArea
        {
            get => clipArea;
            set
            {
                if (clipArea == value)
                    return;

                clipArea = value;
                renderGlyphs?.ForEach(g => { if (g != null) g.ClipArea = clipArea; });
            }
        }

        public void Move(int x, int y)
        {
            renderGlyphs.ForEach(g => { g.X += x; g.Y += y; });
        }

        public void Place(Rect area, TextAlign textAlign)
        {
            int xOffset = -baseX;

            switch (textAlign)
            {
                case TextAlign.Left:
                    baseX = area.Left;
                    break;
                case TextAlign.Center:
                {
                    int offset = (area.Width - totalWidth) / 2;
                    baseX = area.X + offset;
                    break;
                }
                case TextAlign.Right:
                    baseX = area.Right - totalWidth;
                    break;
            }

            xOffset += baseX;

            renderGlyphs.ForEach(g => { g.X += xOffset; g.Y = area.Y; });
        }

        public bool OnScreen => renderGlyphs == null ? false : renderGlyphs.Any(g => g.Visible);

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                    return;

                visible = value;
                renderGlyphs?.ForEach(g => g.Visible = value);
            }
        }

        public void Destroy()
        {
            renderGlyphs?.ForEach(g => g?.Delete());
        }
    }

    class IngameFontProvider : IFontProvider
    {
        class IngameFont : IFont
        {
            readonly Dictionary<uint, Graphic> glyphGraphics;
            readonly IFont digitFonts;

            public int GlyphCount => glyphGraphics.Count;

            // We add 1 "pixel" above but as the graphic is double the resolution
            // we actually have 2 additional pixels inside the graphic to place
            // accents etc.
            public int GlyphHeight => 8;

            public IngameFont(IDataReader fontReader, IFont digitFonts)
            {
                this.digitFonts = digitFonts;
                // 12x14 pixels per glyph
                var data = fontReader.ReadToEnd();
                int glyphCount = data.Length / (14 * 2); // 14 pixels and 2 bytes per glyph width
                glyphGraphics = new(glyphCount);
                // We use 12x16 as it is mapped to the size 6x8.
                // The lower 2 pixels are empty for every character.
                for (uint i = 0; i < glyphCount; ++i)
                    glyphGraphics.Add(i, new Graphic(12, 16, 0));
                int lineStart = 0;
                for (int y = 0; y < 14; ++y)
                {
                    for (int g = 0; g < glyphCount; ++g)
                    {
                        var graphic = glyphGraphics[(uint)g];
                        byte mask = 0x80;
                        int index = lineStart + g * 2;

                        for (int x = 0; x < 8; ++x)
                        {
                            if ((data[index] & mask) != 0)
                                graphic.Data[x + y * 12] = 1;
                            mask >>= 1;
                        }

                        mask = 0x80;
                        ++index;

                        for (int x = 8; x < 12; ++x)
                        {
                            if ((data[index] & mask) != 0)
                                graphic.Data[x + y * 12] = 1;
                            mask >>= 1;
                        }
                    }

                    lineStart += glyphCount * 2;
                }
            }

            public Graphic GetGlyphGraphic(uint glyphIndex) => glyphGraphics[glyphIndex];

            public Graphic GetDigitGlyphGraphic(uint glyphIndex) => digitFonts.GetDigitGlyphGraphic(glyphIndex);
        }

        readonly IngameFont ingameFont;

        public IngameFontProvider(IDataReader fontReader, IFont digitFonts)
        {
            ingameFont = new IngameFont(fontReader, digitFonts);
        }

        public IFont GetFont() => ingameFont;
    }

    class Font
    {
        readonly int spaceWidth;
        readonly IReadOnlyDictionary<char, Glyph> glyphs;
        readonly bool upperOnly;
        readonly List<char> characters;
        readonly uint textureAtlasIndexOffset = 0;

        public Dictionary<uint, Graphic> GlyphGraphics => glyphs.OrderBy(g => g.Key).
            Select((g, i) => new { Glyph = g, Index = i }).ToDictionary(g => textureAtlasIndexOffset + (uint)g.Index,
                g => g.Glyph.Value.Graphic);

        public Font(IReadOnlyDictionary<char, Glyph> glyphs, int spaceWidth, uint textureAtlasIndexOffset)
        {
            this.glyphs = glyphs;
            this.spaceWidth = spaceWidth;
            upperOnly = false;
            characters = glyphs.Keys.OrderBy(k => k).ToList();
            this.textureAtlasIndexOffset = textureAtlasIndexOffset;
        }

        public Font(byte[] data, int spaceWidth)
        {
            var glyphs = new Dictionary<char, Glyph>();
            this.spaceWidth = spaceWidth;
            upperOnly = true;
            var glyphReader = new BinaryReader(new MemoryStream(data));
            Graphic LoadGraphic(int width, int height)
            {
                var graphic = new Graphic(width, height, 0);

                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width / 8; ++x)
                    {
                        byte bits = glyphReader.ReadByte();

                        for (int b = 0; b < 8; ++b)
                        {
                            if ((bits & (1 << (7 - b))) != 0)
                                graphic.Data[x * 8 + b + y * width] = 2; // Color index 2 is white
                        }
                    }
                }

                return graphic;
            }

            while (glyphReader.BaseStream.Position < glyphReader.BaseStream.Length)
            {
                char ch = (char)glyphReader.ReadByte();
                int width = glyphReader.ReadByte();
                int height = glyphReader.ReadByte();
                var glyph = new Glyph
                {
                    Advance = glyphReader.ReadByte(),
                    Graphic = LoadGraphic(width, height)
                };
                glyphs.Add(ch, glyph);
            }

            characters = glyphs.Keys.OrderBy(k => k).ToList();

            this.glyphs = glyphs;
        }

        public Text CreateText(IRenderView renderView, Layer layer, Rect area, string text,
            byte displayLayer, TextAlign textAlign = TextAlign.Center, byte alpha = 255, Rect clipArea = null)
        {
            text = new string(TextProcessor.RemoveDiacritics(text).Where(ch => ch == ' ' || glyphs.ContainsKey(upperOnly ? char.ToUpper(ch) : ch)).ToArray());
            var renderText = new Text(renderView, layer, text, glyphs, characters, displayLayer, spaceWidth, upperOnly,
                textureAtlasIndexOffset, alpha, clipArea);
            renderText.Place(area, textAlign);
            return renderText;
        }

        public int MeasureTextWidth(string text)
        {
            int totalWidth = 0;

            foreach (char ch in text.ToUpper())
            {
                if (ch == ' ')
                    totalWidth += spaceWidth;
                else
                    totalWidth += glyphs[ch].Advance;
            }

            return totalWidth;
        }
    }
}
