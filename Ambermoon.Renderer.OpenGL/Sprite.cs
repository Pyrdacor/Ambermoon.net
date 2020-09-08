/*
 * Sprite.cs - Textured sprite
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

using Ambermoon.Render;

namespace Ambermoon.Renderer
{
    /// <summary>
    /// A sprite has a fixed size and an offset into the layer's texture atlas.
    /// The layer will sort sprites by size and then by the texture atlas offset.
    /// </summary>
    internal class Sprite : RenderNode, ISprite
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        int baseLineOffset = 0;
        byte paletteIndex = 0;

        public Sprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
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

        public virtual Position TextureAtlasOffset
        {
            get => textureAtlasOffset;
            set
            {
                if (textureAtlasOffset == value)
                    return;

                textureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        public int BaseLineOffset
        {
            get => baseLineOffset;
            set
            {
                if (baseLineOffset == value)
                    return;

                baseLineOffset = value;

                UpdatePosition();
            }
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this);
        }

        protected override void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this);
        }

        protected virtual void UpdatePaletteIndex()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePaletteIndex(drawIndex, PaletteIndex);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
        }
    }

    internal class MaskedSprite : RenderNode, IMaskedSprite
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        int baseLineOffset = 0;
        Position maskTextureAtlasOffset = null;
        byte paletteIndex = 0;

        public MaskedSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
            maskTextureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
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

        public Position TextureAtlasOffset
        {
            get => textureAtlasOffset;
            set
            {
                if (textureAtlasOffset == value)
                    return;

                textureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        public int BaseLineOffset
        {
            get => baseLineOffset;
            set
            {
                if (baseLineOffset == value)
                    return;

                baseLineOffset = value;

                UpdatePosition();
            }
        }

        public Position MaskTextureAtlasOffset
        {
            get => maskTextureAtlasOffset;
            set
            {
                if (maskTextureAtlasOffset == value)
                    return;

                maskTextureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this, maskTextureAtlasOffset);
        }

        protected override void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this, maskTextureAtlasOffset);
        }

        protected virtual void UpdatePaletteIndex()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePaletteIndex(drawIndex, PaletteIndex);
        }
    }

    internal class LayerSprite : Sprite, ILayerSprite
    {
        byte displayLayer = 0;

        public LayerSprite(int width, int height, int textureAtlasX, int textureAtlasY, byte displayLayer, Rect virtualScreen)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            this.displayLayer = displayLayer;
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

        protected virtual void UpdateDisplayLayer()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateDisplayLayer(drawIndex, displayLayer);
        }
    }

    internal class MaskedLayerSprite : MaskedSprite, IMaskedLayerSprite
    {
        byte displayLayer = 0;

        public MaskedLayerSprite(int width, int height, int textureAtlasX, int textureAtlasY, byte displayLayer, Rect virtualScreen)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            this.displayLayer = displayLayer;
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

        protected virtual void UpdateDisplayLayer()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateDisplayLayer(drawIndex, displayLayer);
        }
    }

    internal class AnimatedSprite : Sprite, IAnimatedSprite
    {
        Position initialTextureOffset;
        uint currentFrame = 0;

        public AnimatedSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen, uint numFrames, int textureAtlasWidth)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            TextureAtlasWidth = textureAtlasWidth;
            initialTextureOffset = new Position(textureAtlasX, textureAtlasY);
            NumFrames = numFrames;
            CurrentFrame = 0;
        }

        public override Position TextureAtlasOffset
        {
            get => base.TextureAtlasOffset;
            set
            {
                if (TextureAtlasOffset == value)
                    return;

                base.TextureAtlasOffset = value;
                initialTextureOffset = value;
            }
        }
        public int TextureAtlasWidth { get; set; }
        public uint NumFrames { get; set; }
        public uint CurrentFrame
        {
            get => currentFrame;
            set
            {
                if (NumFrames > 1)
                {
                    currentFrame = value % NumFrames;
                    int newTextureOffsetX = initialTextureOffset.X + (int)currentFrame * Width;
                    int newTextureOffsetY = initialTextureOffset.Y;

                    while (newTextureOffsetX >= TextureAtlasWidth)
                    {
                        newTextureOffsetX -= TextureAtlasWidth;
                        newTextureOffsetY += Height;
                    }

                    base.TextureAtlasOffset = new Position(newTextureOffsetX, newTextureOffsetY);
                }
                else
                {
                    currentFrame = 0;
                }
            }
        }
    }

    internal class TextCharacterSprite : LayerSprite
    {
        public byte TextColorIndex { get; set; }

        public TextCharacterSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen, byte displayLayer)
            : base(width, height, textureAtlasX, textureAtlasY, displayLayer, virtualScreen)
        {

        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this, null, TextColorIndex);
        }

        public void UpdateTextColorIndex()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextColorIndex(drawIndex, TextColorIndex);
        }
    }

    public class SpriteFactory : ISpriteFactory
    {
        readonly Rect virtualScreen = null;

        public SpriteFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public ISprite Create(int width, int height, bool masked, bool layered, byte displayLayer = 0)
        {
            if (masked)
            {
                if (layered)
                    return new MaskedLayerSprite(width, height, 0, 0, displayLayer, virtualScreen);
                else
                    return new MaskedSprite(width, height, 0, 0, virtualScreen);
            }
            else
            {
                if (layered)
                    return new LayerSprite(width, height, 0, 0, displayLayer, virtualScreen);
                else
                    return new Sprite(width, height, 0, 0, virtualScreen);
            }
        }

        public IAnimatedSprite CreateAnimated(int width, int height, int textureAtlasWidth, uint numFrames = 1)
        {
            return new AnimatedSprite(width, height, 0, 0, virtualScreen, numFrames, textureAtlasWidth);
        }
    }
}
