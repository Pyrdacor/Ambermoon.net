using Ambermoon.Data;
using Ambermoon.Render;
using System.Collections.Generic;
using System.IO;

namespace Ambermoon
{
    struct Glyph
    {
        public int Advance;
        public Graphic Graphic;
        public const int SpaceWidth = 12;
    }

    class IntroText
    {
        readonly List<ILayerSprite> renderGlyphs = new List<ILayerSprite>(26);
        bool visible = false;
        readonly int totalWidth = 0;
        int baseX = 0;
        byte colorIndex = 2; // white

        public IntroText(IRenderView renderView, string text, Dictionary<char, Glyph> glyphs, byte displayLayer)
        {
            totalWidth = 0;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroText);

            foreach (char ch in text.ToUpper())
            {
                if (ch == ' ')
                    totalWidth += Glyph.SpaceWidth;
                else if (ch >= 'A' && ch <= 'Z')
                {
                    var glyph = glyphs[ch];
                    var sprite = renderView.SpriteFactory.Create(glyph.Graphic.Width, glyph.Graphic.Height, true, displayLayer) as ILayerSprite;
                    sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)(ch - 'A'));
                    sprite.X = totalWidth;
                    sprite.Y = 0;
                    sprite.Layer = renderView.GetLayer(Layer.IntroText);
                    sprite.PaletteIndex = 50;
                    sprite.Visible = false;
                    renderGlyphs.Add(sprite);
                    totalWidth += glyph.Advance;
                }
                else
                    throw new AmbermoonException(ExceptionScope.Data, $"Unsupported character: {ch}");
            }
        }

        public byte ColorIndex
        {
            get => colorIndex;
            set
            {
                if (colorIndex == value)
                    return;

                colorIndex = value;
                renderGlyphs?.ForEach(g => { if (g != null) g.MaskColor = colorIndex; });
            }
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

    class IntroFont
    {
        readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>(26);

        public Dictionary<uint, Graphic> GlyphGraphics
        {
            get
            {
                var glyphGraphics = new Dictionary<uint, Graphic>(26);

                for (uint i = 0; i < 26; ++i)
                    glyphGraphics.Add(i, glyphs[(char)('A' + i)].Graphic);

                return glyphGraphics;
            }
        }

        public IntroFont()
        {
            var glyphReader = new BinaryReader(new MemoryStream(Resources.IntroFont));
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

            for (int i = 0; i < 26; ++i)
            {
                char ch = (char)('A' + i);

                if (ch != (char)glyphReader.ReadByte())
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid intro font data.");

                int width = glyphReader.ReadByte();
                int height = glyphReader.ReadByte();
                var glyph = new Glyph
                {
                    Advance = glyphReader.ReadByte(),
                    Graphic = LoadGraphic(width, height)
                };
                glyphs.Add(ch, glyph);
            }
        }

        public IntroText CreateText(IRenderView renderView, Rect area, string text, byte displayLayer, TextAlign textAlign = TextAlign.Center)
        {
            var introText = new IntroText(renderView, text, glyphs, displayLayer);
            introText.Place(area, textAlign);
            return introText;
        }

        public int MeasureTextWidth(string text)
        {
            int totalWidth = 0;

            foreach (char ch in text.ToUpper())
            {
                if (ch == ' ')
                    totalWidth += Glyph.SpaceWidth;
                else if (ch >= 'A' && ch <= 'Z')
                    totalWidth += glyphs[ch].Advance;
                else
                    throw new AmbermoonException(ExceptionScope.Data, $"Unsupported character: {ch}");
            }

            return totalWidth;
        }
    }
}
