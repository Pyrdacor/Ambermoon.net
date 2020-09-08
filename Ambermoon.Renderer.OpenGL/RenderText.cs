/*
 * RenderText.cs - Text render object
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Render;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Renderer
{
    internal class RenderText : RenderNode, IRenderText
    {
        const int CharacterWidth = 6;
        const int CharacterHeight = 6;
        const int LineHeight = 8;
        const byte ShadowColorIndex = 1;
        protected int drawIndex = -1;
        byte displayLayer = 0;
        TextColor textColor;
        bool shadow;
        IText text;
        Rect bounds;
        TextAlign textAlign = TextAlign.Left;
        readonly List<TextCharacterSprite> characterShadowSprites = new List<TextCharacterSprite>();
        readonly List<TextCharacterSprite> characterSprites = new List<TextCharacterSprite>();
        readonly Dictionary<byte, Position> glyphTextureMapping;
        Position[] characterPositions = null;
        int lastCharacterToRender = -1;
        bool updatingPositions = false;

        public RenderText(Rect virtualScreen, Dictionary<byte, Position> glyphTextureMapping)
            : base(0, 0, virtualScreen)
        {
            this.glyphTextureMapping = glyphTextureMapping;
            bounds = virtualScreen;
        }

        public RenderText(Rect virtualScreen, Dictionary<byte, Position> glyphTextureMapping,
            IRenderLayer layer, IText text, TextColor textColor, bool shadow)
            : base(text.MaxLineSize * CharacterWidth, text.LineCount * CharacterHeight - (LineHeight - CharacterHeight), virtualScreen)
        {
            this.glyphTextureMapping = glyphTextureMapping;
            bounds = virtualScreen;
            Layer = layer;
            Text = text;
            TextColor = textColor;
            Shadow = shadow;
        }

        public RenderText(Rect virtualScreen, Dictionary<byte, Position> glyphTextureMapping,
            IRenderLayer layer, IText text, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign)
            : base(text.MaxLineSize * CharacterWidth, text.LineCount * CharacterHeight - (LineHeight - CharacterHeight), virtualScreen)
        {
            this.glyphTextureMapping = glyphTextureMapping;
            this.bounds = bounds;
            this.textAlign = textAlign;
            Layer = layer;
            Text = text;
            TextColor = textColor;
            Shadow = shadow;
            X = bounds.Left;
            Y = bounds.Top;
        }

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                UpdateDisplayLayer();
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

                foreach (var sprite in characterSprites)
                {
                    sprite.TextColorIndex = (byte)textColor;
                    sprite.UpdateTextColorIndex();
                }
            }
        }
        public bool Shadow
        {
            get => shadow;
            set
            {
                if (shadow == value)
                    return;

                shadow = value;
                UpdateTextSprites();
            }
        }
        public IText Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                UpdateTextSprites();
            }
        }

        void UpdateDisplayLayer()
        {
            byte textDisplayLayer = (byte)Util.Min(255, DisplayLayer + 2); // draw above shadow a bit

            characterSprites.ForEach(s => s.DisplayLayer = textDisplayLayer);
            characterShadowSprites.ForEach(s => s.DisplayLayer = DisplayLayer);
        }

        bool UpdateCharacterPositions()
        {
            if (updatingPositions)
                return false;

            updatingPositions = true;

            if (X == short.MaxValue || Y == short.MaxValue) // not on screen
            {
                lastCharacterToRender = -1;
                characterPositions = null;
                Resize(0, 0);
                updatingPositions = false;
                return false;
            }

            int x = X;
            int y = Y;
            int width = 0;
            int lastWhitespaceIndex = -1;
            int lastLineBreakIndex = -1;
            characterPositions = new Position[text.GlyphIndices.Length];
            lastCharacterToRender = -1;
            int numEmptyCharacterInLine = 0; // e.g. color swaps

            bool NewLine()
            {
                if (x > width)
                    width = x;

                x = X;
                y += LineHeight;
                numEmptyCharacterInLine = 0;

                return y + CharacterHeight - 1 < bounds.Bottom;
            }

            void AdjustLineAlign(int lineEndGlyphIndex)
            {
                if (textAlign == TextAlign.Left)
                    return;

                int remainingWidth = bounds.Width - (lineEndGlyphIndex - lastLineBreakIndex - numEmptyCharacterInLine) * CharacterWidth;
                int adjustment = textAlign == TextAlign.Right
                    ? remainingWidth
                    : remainingWidth / 2; // center

                for (int i = lastLineBreakIndex + 1; i <= lineEndGlyphIndex; ++i)
                {
                    if (characterPositions[i] != null)
                        characterPositions[i].X += adjustment;
                }
            }

            for (int i = 0; i < text.GlyphIndices.Length; ++i)
            {
                byte glyphIndex = text.GlyphIndices[i];

                if (glyphIndex >= (byte)SpecialGlyph.FirstColor)
                {
                    ++numEmptyCharacterInLine;
                    continue;
                }

                // Space is not rendered.
                // $ is used for non-breaking (hard) space. Also not rendered.
                // ^ is used as a line-break. Also not rendered.
                if (glyphIndex == (byte)SpecialGlyph.SoftSpace || glyphIndex == (byte)SpecialGlyph.HardSpace)
                {
                    lastWhitespaceIndex = i;
                    x += CharacterWidth;

                    if (x + CharacterWidth - 1 >= bounds.Right)
                    {
                        AdjustLineAlign(i - 1);
                        lastLineBreakIndex = i;

                        if (!NewLine())
                            break; // nothing more to draw
                    }

                    continue;
                }
                if (glyphIndex == (byte)SpecialGlyph.NewLine)
                {
                    lastWhitespaceIndex = i;
                    AdjustLineAlign(i - 1);
                    lastLineBreakIndex = i;

                    if (!NewLine())
                        break; // nothing more to draw

                    continue;
                }

                characterPositions[i] = new Position(x, y);

                x += CharacterWidth;

                if (x - 1 >= bounds.Right) // the character didn't fit into the line -> move the whole word to next line
                {
                    bool nextLineVisible = NewLine();
                    AdjustLineAlign(lastWhitespaceIndex - 1);
                    lastLineBreakIndex = lastWhitespaceIndex;

                    for (int j = lastWhitespaceIndex + 1; j <= i; ++j)
                    {
                        if (nextLineVisible)
                            characterPositions[i] = null;
                        else                            
                        {
                            characterPositions[i] = new Position(x, y);
                            x += CharacterWidth;
                        }
                    }

                    if (!nextLineVisible)
                    {
                        break; // nothing more to draw
                    }
                }
            }

            if (x > width)
                width = x;

            Resize(width, y + CharacterHeight - Y);

            lastCharacterToRender = characterPositions.Select((pos, index) => new { pos, index }).LastOrDefault(p => p.pos != null)?.index ?? -1;

            AdjustLineAlign(lastCharacterToRender);

            updatingPositions = false;

            return true;
        }

        void UpdateTextSprites()
        {
            characterSprites.ForEach(s => s?.Delete());
            characterSprites.Clear();
            characterShadowSprites.ForEach(s => s?.Delete());
            characterShadowSprites.Clear();

            if (!UpdateCharacterPositions())
                return;

            byte colorIndex = (byte)TextColor;

            for (int i = 0; i <= lastCharacterToRender; ++i)
            {
                byte glyphIndex = text.GlyphIndices[i];

                if (glyphIndex >= (byte)SpecialGlyph.FirstColor)
                    colorIndex = (byte)(glyphIndex - SpecialGlyph.FirstColor);

                if (characterPositions[i] == null)
                    continue;

                var position = characterPositions[i];
                var textureCoord = glyphTextureMapping[glyphIndex];
                var sprite = new TextCharacterSprite(CharacterWidth, CharacterHeight, textureCoord.X, textureCoord.Y, virtualScreen,
                    (byte)Util.Min(255, DisplayLayer + 2)) // ensure to draw it in front of the shadow
                {
                    TextColorIndex = colorIndex,
                    X = position.X,
                    Y = position.Y,
                    Layer = Layer,
                    PaletteIndex = 50,
                    Visible = Visible                    
                };

                characterSprites.Add(sprite);
            }

            if (Shadow)
            {
                foreach (var characterSprite in characterSprites)
                {
                    var shadowSprite = new TextCharacterSprite(CharacterWidth, CharacterHeight,
                        characterSprite.TextureAtlasOffset.X, characterSprite.TextureAtlasOffset.Y,
                        virtualScreen, DisplayLayer)
                    {
                        TextColorIndex = ShadowColorIndex,
                        X = characterSprite.X + 1,
                        Y = characterSprite.Y + 1,
                        Layer = Layer,
                        PaletteIndex = 50,                        
                        Visible = Visible
                    };

                    characterShadowSprites.Add(shadowSprite);
                }
            }
        }

        public void Place(int x, int y)
        {
            Place(new Rect(x, y, virtualScreen.Right - x, virtualScreen.Bottom - y));
        }

        public void Place(Rect rect, TextAlign textAlign = TextAlign.Left)
        {
            if (X != rect.Left || Y != rect.Top || bounds != rect || this.textAlign != textAlign)
            {
                X = rect.Left;
                Y = rect.Top;
                bounds = rect;
                this.textAlign = textAlign;
                UpdateTextSprites();
            }
        }

        protected override void OnVisibilityChanged()
        {
            if (Visible)
                UpdateTextSprites(); // this automatically set sprite.Visible to true
            else
                RemoveFromLayer();
        }

        protected override void AddToLayer()
        {
            foreach (var sprite in characterShadowSprites)
                sprite.Visible = true;
            foreach (var sprite in characterSprites)
                sprite.Visible = true;
        }

        protected override void RemoveFromLayer()
        {
            foreach (var sprite in characterShadowSprites)
                sprite.Visible = false;
            foreach (var sprite in characterSprites)
                sprite.Visible = false;
        }

        protected override void UpdatePosition()
        {
            UpdateTextSprites();

            foreach (var sprite in characterShadowSprites)
                sprite.Visible = Visible;
            foreach (var sprite in characterSprites)
                sprite.Visible = Visible;
        }
    }

    public class RenderTextFactory : IRenderTextFactory
    {
        readonly Rect virtualScreen = null;

        public RenderTextFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public Dictionary<byte, Position> GlyphTextureMapping { get; set; }

        public IRenderText Create()
        {
            return new RenderText(virtualScreen, GlyphTextureMapping);
        }

        public IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow)
        {
            return new RenderText(virtualScreen, GlyphTextureMapping, layer, text, textColor, shadow);
        }

        public IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left)
        {
            return new RenderText(virtualScreen, GlyphTextureMapping, layer, text, textColor, shadow, bounds, textAlign);
        }
    }
}
