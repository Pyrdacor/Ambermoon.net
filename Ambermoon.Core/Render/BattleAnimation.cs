using System;
using System.Linq;

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
        float endScale = 1.0f;
        float startScale = 1.0f;
        int endY;
        int startY;
        public bool Finished { get; private set; } = true;

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

        public void Play(int[] frameIndices, uint ticksPerFrame, uint ticks, int? endY = null, float? endScale = null)
        {
            Finished = false;
            this.frameIndices = frameIndices;
            this.ticksPerFrame = ticksPerFrame;
            startScale = scale;
            this.endScale = endScale ?? startScale;
            startY = baseSpriteLocation.Y;
            this.endY = endY ?? startY;
            startAnimationTicks = ticks;
            sprite.DisplayLayer += (byte)((this.endY - startY) * 6 * 5);
        }

        public void PlayWithoutAnimating(uint durationInTicks, uint ticks, int? endY = null, float? endScale = null)
        {
            Play(new int[] { 0 }, durationInTicks, ticks, endY, endScale);
        }

        public void Reset()
        {
            sprite.TextureAtlasOffset = baseTextureCoords;
            Finished = true;
        }

        public bool Update(uint ticks)
        {
            if (ticksPerFrame == 0)
            {
                Finished = true;
                AnimationFinished?.Invoke();
                return false;
            }

            uint elapsed = ticks - startAnimationTicks;
            uint frame = elapsed / ticksPerFrame;

            if (frame >= frameIndices.Length)
            {
                baseSpriteLocation.Y = endY; // TODO: respect scale here?
                Position = baseSpriteLocation;
                Scale = endScale;
                Finished = true;
                AnimationFinished?.Invoke();
                return false;
            }

            float animationTime = frameIndices.Length * ticksPerFrame;
            float factor = elapsed / animationTime;
            baseSpriteLocation.Y = startY + Util.Round((endY - startY) * factor); // TODO: respect scale here?
            Position = baseSpriteLocation;
            Scale = startScale + (endScale - startScale) * factor;
            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frameIndices[frame] * baseSpriteSize.Width, 0);

            return true;
        }

        public void SetDisplayLayer(byte displayLayer)
        {
            sprite.DisplayLayer = displayLayer;
        }
    }
}
