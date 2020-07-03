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
    internal class Sprite : Node, ISprite
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        int baseLineOffset = 0;

        public Sprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
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

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
        }
    }

    internal class MaskedSprite : Node, IMaskedSprite
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        int baseLineOffset = 0;
        Position maskTextureAtlasOffset = null;

        public MaskedSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
            maskTextureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
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
        readonly int textureAtlasWidth;
        uint currentFrame = 0;

        public AnimatedSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen, uint numFrames, int textureAtlasWidth)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            this.textureAtlasWidth = textureAtlasWidth;
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

                    while (newTextureOffsetX >= textureAtlasWidth)
                    {
                        newTextureOffsetX -= textureAtlasWidth;
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

    public class SpriteFactory : ISpriteFactory
    {
        readonly Rect virtualScreen = null;

        public SpriteFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public ISprite Create(int width, int height, int textureAtlasX, int textureAtlasY, bool masked, bool layered, byte displayLayer = 0)
        {
            if (masked)
            {
                if (layered)
                    return new MaskedLayerSprite(width, height, textureAtlasX, textureAtlasY, displayLayer, virtualScreen);
                else
                    return new MaskedSprite(width, height, textureAtlasX, textureAtlasY, virtualScreen);
            }
            else
            {
                if (layered)
                    return new LayerSprite(width, height, textureAtlasX, textureAtlasY, displayLayer, virtualScreen);
                else
                    return new Sprite(width, height, textureAtlasX, textureAtlasY, virtualScreen);
            }
        }

        public IAnimatedSprite CreateAnimated(int width, int height, int textureAtlasX, int textureAtlasY, int textureAtlasWidth, uint numFrames = 1)
        {
            return new AnimatedSprite(width, height, textureAtlasX, textureAtlasY, virtualScreen, numFrames, textureAtlasWidth);
        }
    }
}
