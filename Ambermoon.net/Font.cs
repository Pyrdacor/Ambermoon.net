using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon
{
    class Text
    {
        readonly List<ILayerSprite> renderGlyphs = new List<ILayerSprite>();
        bool visible = false;
        readonly int totalWidth = 0;
        int baseX = 0;
        TextColor textColor = TextColor.White;

        public Text(IRenderView renderView, Layer layer, string text, IReadOnlyDictionary<char, Glyph> glyphs,
            List<char> characters, byte displayLayer, int spaceWidth, bool upperOnly, uint textureAtlasIndexOffset)
        {
            totalWidth = 0;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(layer);

            if (upperOnly)
                text = text.ToUpper();

            foreach (char ch in text)
            {
                if (ch == ' ')
                    totalWidth += spaceWidth;
                else if (glyphs.TryGetValue(ch, out var glyph))
                {
                    var sprite = renderView.SpriteFactory.Create(glyph.Graphic.Width, glyph.Graphic.Height, true, displayLayer) as ILayerSprite;
                    sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)characters.IndexOf(ch) + textureAtlasIndexOffset);
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
            byte displayLayer, TextAlign textAlign = TextAlign.Center)
        {
            var renderText = new Text(renderView, layer, text, glyphs, characters, displayLayer, spaceWidth, upperOnly,
                textureAtlasIndexOffset);
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
