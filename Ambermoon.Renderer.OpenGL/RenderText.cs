/*
 * RenderText.cs - Text render object
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.Renderer
{
    internal class RenderText : RenderNode, IRenderText
    {
        const int CharacterWidth = 6;
        const int CharacterHeight = 6;
        const int LineHeight = 7;
        const byte ShadowColorIndex = (byte)TextColor.Black;
        protected int drawIndex = -1;
        byte displayLayer = 0;
        byte paletteIndex = 49;
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
        protected readonly int characterWidth = CharacterWidth;
        protected readonly int characterHeight = CharacterHeight;

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
            IRenderLayer layer, IText text, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign,
            int characterWidth = CharacterWidth, int characterHeight = CharacterHeight)
            : base(text.MaxLineSize * characterWidth, text.LineCount * characterHeight - (LineHeight - characterHeight), virtualScreen)
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
            this.characterWidth = characterWidth;
            this.characterHeight = characterHeight;
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

        public byte PaletteIndex
        {
            get => paletteIndex;
            set
            {
                if (paletteIndex == value)
                    return;

                paletteIndex = value;

                UpdatePaletteIndex();
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

        public TextAlign TextAlign
        {
            get => textAlign;
            set
            {
                if (textAlign == value)
                    return;

                textAlign = value;

                UpdateTextSprites();
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

        private protected override bool CheckOnScreen()
        {
            return CheckOnScreen(bounds);
        }

        void UpdateDisplayLayer()
        {
            byte textDisplayLayer = (byte)Util.Min(255, DisplayLayer + 2); // draw above shadow a bit

            characterSprites.ForEach(s => s.DisplayLayer = textDisplayLayer);
            characterShadowSprites.ForEach(s => s.DisplayLayer = DisplayLayer);
        }

        void UpdatePaletteIndex()
        {
            characterSprites.ForEach(s => s.PaletteIndex = PaletteIndex);
            characterShadowSprites.ForEach(s => s.PaletteIndex = PaletteIndex);
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
                if (x > X + width)
                    width = x - X;

                x = X;
                y += LineHeight;
                numEmptyCharacterInLine = 0;

                return y + characterHeight - 1 <= bounds.Bottom;
            }

            void AdjustLineAlign(int lineEndGlyphIndex)
            {
                if (textAlign == TextAlign.Left)
                    return;

                int remainingWidth = bounds.Width - (lineEndGlyphIndex - lastLineBreakIndex - numEmptyCharacterInLine) * characterWidth;
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

                if (glyphIndex >= (byte)SpecialGlyph.NoTrim)
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
                    x += characterWidth;

                    if (x + characterWidth - 1 >= bounds.Right)
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

                x += characterWidth;

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
                            x += characterWidth;
                        }
                    }

                    if (!nextLineVisible)
                    {
                        break; // nothing more to draw
                    }
                }
            }

            if (x - bounds.Left > width)
                width = x - bounds.Left;

            Resize(width, y + characterHeight - Y);

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

            if (text == null)
                return;

            if (!UpdateCharacterPositions())
                return;

            byte colorIndex = (byte)TextColor;

            for (int i = 0; i <= lastCharacterToRender; ++i)
            {
                byte glyphIndex = text.GlyphIndices[i];

                if (glyphIndex == (byte)SpecialGlyph.NoTrim)
                    continue;

                if (glyphIndex >= (byte)SpecialGlyph.FirstColor)
                {
                    colorIndex = (byte)(glyphIndex - SpecialGlyph.FirstColor);

                    if (colorIndex == 32) // Special -> default color
                        colorIndex = (byte)TextColor;
                }

                if (characterPositions[i] == null)
                    continue;

                var position = characterPositions[i];
                var textureCoord = glyphTextureMapping[glyphIndex];
                var sprite = new TextCharacterSprite(characterWidth, characterHeight, textureCoord.X, textureCoord.Y, virtualScreen,
                    (byte)Util.Min(255, DisplayLayer + 2)) // ensure to draw it in front of the shadow
                {
                    TextColorIndex = colorIndex,
                    X = position.X,
                    Y = position.Y,
                    Layer = Layer,
                    PaletteIndex = PaletteIndex,
                    Visible = Visible
                };
                sprite.ClipArea = ClipArea;

                characterSprites.Add(sprite);
            }

            if (Shadow)
            {
                foreach (var characterSprite in characterSprites)
                {
                    var shadowSprite = new TextCharacterSprite(characterWidth, characterHeight,
                        characterSprite.TextureAtlasOffset.X, characterSprite.TextureAtlasOffset.Y,
                        virtualScreen, DisplayLayer)
                    {
                        TextColorIndex = ShadowColorIndex,
                        X = characterSprite.X + 1,
                        Y = characterSprite.Y + 1,
                        Layer = Layer,
                        PaletteIndex = PaletteIndex,                        
                        Visible = Visible
                    };
                    shadowSprite.ClipArea = ClipArea;

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
                sprite.Visible = Visible && sprite.InsideClipArea(ClipArea);
            foreach (var sprite in characterSprites)
                sprite.Visible = Visible && sprite.InsideClipArea(ClipArea);
        }

        protected override void OnClipAreaChanged(bool onScreen, bool needUpdate)
        {
            if (onScreen && needUpdate)
            {
                UpdateTextSprites();
            }
        }
    }

    public class RenderTextFactory : IRenderTextFactory
    {
        internal Rect VirtualScreen { get; private set; } = null;

        public RenderTextFactory(Rect virtualScreen)
        {
            VirtualScreen = virtualScreen;
        }

        /// <inheritdoc/>
        public Dictionary<byte, Position> GlyphTextureMapping { get; set; }
        /// <inheritdoc/>
        public Dictionary<byte, Position> DigitGlyphTextureMapping { get; set; }

        public IRenderText Create()
        {
            return new RenderText(VirtualScreen, GlyphTextureMapping);
        }

        public IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow)
        {
            return new RenderText(VirtualScreen, GlyphTextureMapping, layer, text, textColor, shadow);
        }

        public IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left)
        {
            return new RenderText(VirtualScreen, GlyphTextureMapping, layer, text, textColor, shadow, bounds, textAlign);
        }

        public IRenderText CreateDigits(IRenderLayer layer, IText digits, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left)
        {
            return new DigitText(VirtualScreen, DigitGlyphTextureMapping, layer, digits, textColor, shadow, bounds, textAlign);
        }

        private class DigitText : RenderText
        {
            const int DigitWidth = 5;
            const int DigitHeight = 5;

            public DigitText(Rect virtualScreen, Dictionary<byte, Position> glyphTextureMapping,
                IRenderLayer layer, IText digits, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign)
                : base(virtualScreen, glyphTextureMapping, layer, digits, textColor, shadow, bounds, textAlign, DigitWidth, DigitHeight)
            {

            }
        }
    }
}
