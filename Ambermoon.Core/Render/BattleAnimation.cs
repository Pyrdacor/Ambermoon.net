using System;

namespace Ambermoon.Render
{
    internal class BattleAnimation
    {
        Position baseSpriteLocation;
        readonly Size baseSpriteSize;
        readonly ILayerSprite sprite;
        readonly Position baseTextureCoords;
        uint startAnimationTicks;
        uint ticksPerFrame;
        int[] frameIndices;
        float scale = 1.0f;

        public event Action AnimationFinished;

        public BattleAnimation(ILayerSprite sprite, float initialScale = 1.0f)
        {
            baseSpriteLocation = new Position(sprite.X, sprite.Y);
            baseSpriteSize = new Size(sprite.Width, sprite.Height);
            this.sprite = sprite;
            baseTextureCoords = new Position(sprite.TextureAtlasOffset);
            sprite.TextureSize = baseSpriteSize;
            Scale = initialScale;
            // Scaling might change location but we don't want this on
            // initial placement so ensure correct one here again.
            Position = baseSpriteLocation;
        }

        public Position Position
        {
            get => new Position(sprite.X, sprite.Y);
            set
            {
                sprite.X = value.X;
                sprite.Y = value.Y;
            }
        }

        public float Scale
        {
            get => scale;
            set
            {
                if (value == scale)
                    return;

                scale = value;

                int newWidth = Util.Round(baseSpriteSize.Width * scale);
                int newHeight = Util.Round(baseSpriteSize.Height * scale);

                int xOffset = (baseSpriteSize.Width - newWidth) / 2;
                int yOffset = (baseSpriteSize.Height - newHeight) / 2;

                Position = baseSpriteLocation + new Position(xOffset, yOffset);
                sprite.Resize(newWidth, newHeight);
            }
        }

        public void Destroy() => sprite?.Delete();

        public void MoveAndScale(int x, int y, float yFactorPerPixel)
        {
            var offset = new Position(x, y);
            baseSpriteLocation += offset;
            Position += offset;
            Scale += y * yFactorPerPixel;
        }

        public void Play(int[] frameIndices, uint ticksPerFrame, uint ticks)
        {
            this.frameIndices = frameIndices;
            this.ticksPerFrame = ticksPerFrame;
            startAnimationTicks = ticks;
        }

        public void Reset()
        {
            sprite.TextureAtlasOffset = baseTextureCoords;
        }

        public bool Update(uint ticks)
        {
            if (ticksPerFrame == 0)
            {
                AnimationFinished?.Invoke();
                return false;
            }

            uint elapsed = ticks - startAnimationTicks;
            uint frame = elapsed / ticksPerFrame;

            if (frame >= frameIndices.Length)
            {
                AnimationFinished?.Invoke();
                return false;
            }

            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frameIndices[frame] * baseSpriteSize.Width, 0);

            return true;
        }
    }
}
