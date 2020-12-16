using System;

namespace Ambermoon.Render
{
    internal class BattleAnimation
    {
        public enum AnimationScaleType
        {
            None,
            XOnly,
            YOnly,
            Both
        }

        public enum HorizontalAnchor
        {
            Left,
            Center,
            Right
        }

        public enum VerticalAnchor
        {
            Top,
            Center,
            Bottom
        }

        // Note: Positions are always center positions
        Position baseSpriteLocation;
        Size baseSpriteSize;
        readonly ILayerSprite sprite;
        Position baseTextureCoords;
        uint startAnimationTicks;
        uint ticksPerFrame;
        int[] frameIndices;
        float scale = 1.0f;
        float endScale = 1.0f;
        float startScale = 1.0f;
        int endX;
        int endY;
        int startX;
        int startY;
        public HorizontalAnchor AnchorX { get; set; } = HorizontalAnchor.Center;
        public VerticalAnchor AnchorY { get; set; } = VerticalAnchor.Center;
        public bool Finished { get; private set; } = true;
        public AnimationScaleType ScaleType { get; set; } = AnimationScaleType.Both;

        public event Action AnimationFinished;
        public event Action<float> AnimationUpdated;

        public BattleAnimation(ILayerSprite sprite)
        {
            baseSpriteLocation = new Position(sprite.X + sprite.Width / 2, sprite.Y + sprite.Height / 2);
            baseSpriteSize = new Size(sprite.Width, sprite.Height);
            this.sprite = sprite;
            baseTextureCoords = new Position(sprite.TextureAtlasOffset);
            sprite.TextureSize ??= baseSpriteSize;
            Scale = 1.0f;
            sprite.ClipArea = Global.CombatBackgroundArea;
        }

        public void SetStartFrame(Position textureOffset, Size size, Position centerPosition = null,
            float initialScale = 1.0f, bool mirrorX = false, Size customTextureSize = null,
            HorizontalAnchor anchorX = HorizontalAnchor.Center, VerticalAnchor anchorY = VerticalAnchor.Center)
        {
            if (centerPosition != null)
                baseSpriteLocation = new Position(centerPosition);
            baseSpriteSize = new Size(size);
            baseTextureCoords = new Position(textureOffset);
            sprite.TextureAtlasOffset = new Position(textureOffset);
            sprite.TextureSize = customTextureSize ?? baseSpriteSize;
            sprite.MirrorX = mirrorX;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Scale = initialScale;
        }

        public void SetStartFrame(Position centerPosition, float initialScale = 1.0f)
        {
            if (centerPosition != null)
                baseSpriteLocation = new Position(centerPosition);
            Scale = initialScale;
        }

        public bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = value;
        }

        Position Position
        {
            set
            {
                sprite.X = value.X;
                sprite.Y = value.Y;
            }
        }

        float Scale
        {
            set
            {
                scale = value;

                int newWidth = ScaleType switch
                {
                    AnimationScaleType.None => baseSpriteSize.Width,
                    AnimationScaleType.YOnly => baseSpriteSize.Width,
                    _ => Util.Round(baseSpriteSize.Width * scale)
                };
                int newHeight = ScaleType switch
                {
                    AnimationScaleType.None => baseSpriteSize.Height,
                    AnimationScaleType.XOnly => baseSpriteSize.Height,
                    _ => Util.Round(baseSpriteSize.Height * scale)
                };
                int newX = AnchorX switch
                {
                    HorizontalAnchor.Left => baseSpriteLocation.X,
                    HorizontalAnchor.Right => baseSpriteLocation.X + (sprite.Width - newWidth) / 2,
                    _ => baseSpriteLocation.X - newWidth / 2
                };
                int newY = AnchorY switch
                {
                    VerticalAnchor.Top => baseSpriteLocation.Y,
                    VerticalAnchor.Bottom => baseSpriteLocation.Y + (sprite.Height - newHeight) / 2,
                    _ => baseSpriteLocation.Y - newHeight / 2
                };
                Position = new Position(newX, newY);
                sprite.Resize(newWidth, newHeight);
            }
        }

        public void Destroy() => sprite?.Delete();

        public void Play(int[] frameIndices, uint ticksPerFrame, uint ticks, Position endPosition = null, float? endScale = null)
        {
            Finished = false;
            this.frameIndices = frameIndices;
            this.ticksPerFrame = ticksPerFrame;
            startScale = scale;
            this.endScale = endScale ?? startScale;
            startX = baseSpriteLocation.X;
            startY = baseSpriteLocation.Y;
            endX = endPosition?.X ?? startX;
            endY = endPosition?.Y ?? startY;
            startAnimationTicks = ticks;
        }

        public void PlayWithoutAnimating(uint durationInTicks, uint ticks, Position endPosition = null, float? endScale = null)
        {
            Play(new int[] { 0 }, durationInTicks, ticks, endPosition, endScale);
        }

        public void Reset(int frame = 0)
        {
            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frame * baseSpriteSize.Width, 0);
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
                baseSpriteLocation.X = endX;
                baseSpriteLocation.Y = endY;
                Scale = endScale; // Note: scale will also set the new position
                Finished = true;
                AnimationFinished?.Invoke();
                return false;
            }

            float animationTime = frameIndices.Length * ticksPerFrame;
            float factor = elapsed / animationTime;
            baseSpriteLocation.X = startX + Util.Round((endX - startX) * factor);
            baseSpriteLocation.Y = startY + Util.Round((endY - startY) * factor);
            Scale = startScale + (endScale - startScale) * factor; // Note: scale will also set the new position
            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frameIndices[frame] * sprite.TextureSize.Width, 0);
            AnimationUpdated?.Invoke(factor);

            return true;
        }

        public void SetDisplayLayer(byte displayLayer)
        {
            sprite.DisplayLayer = displayLayer;
        }
    }
}
