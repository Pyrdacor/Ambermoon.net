/*
 * BattleAnimation.cs - Animations in battle
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
        readonly int textureFactor;
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
        bool wasVisible;
        public HorizontalAnchor AnchorX { get; set; } = HorizontalAnchor.Center;
        public VerticalAnchor AnchorY { get; set; } = VerticalAnchor.Center;
        public bool Finished { get; private set; } = true;
        public AnimationScaleType ScaleType { get; set; } = AnimationScaleType.Both;
        /// <summary>
        /// When scaling the center point is in relation to this reference scale.
        /// </summary>
        public float ReferenceScale { get; set; } = 1.0f;

        public event Action AnimationFinished;
        public event Action<float> AnimationUpdated;

        public BattleAnimation(ILayerSprite sprite)
        {
            baseSpriteLocation = new Position(sprite.X + sprite.Width / 2, sprite.Y + sprite.Height / 2);
            baseSpriteSize = new Size(sprite.Width, sprite.Height);
            this.sprite = sprite;
            textureFactor = (int)(sprite.Layer?.TextureFactor ?? 1);
            baseTextureCoords = new Position(sprite.TextureAtlasOffset);
            sprite.TextureSize ??= baseSpriteSize;
            Scale = 1.0f;
            sprite.ClipArea ??= Global.CombatBackgroundArea;
            wasVisible = sprite.Visible;
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
            Scale = startScale = initialScale;
        }

        public void SetStartFrame(Position centerPosition, float initialScale = 1.0f)
        {
            if (centerPosition != null)
                baseSpriteLocation = new Position(centerPosition);
            Scale = startScale = initialScale;
        }

        public bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = wasVisible = value;
        }

        Position Position
        {
            set
            {
                sprite.X = value.X;
                sprite.Y = value.Y;
            }
        }

        public float Scale
        {
            get => scale;
            private set
            {
                scale = value;

                var baseLocation = new Position(baseSpriteLocation);
                float refScaleX = ScaleType switch
                {
                    AnimationScaleType.None => 1.0f,
                    AnimationScaleType.YOnly => 1.0f,
                    _ => ReferenceScale
                };
                float refScaleY = ScaleType switch
                {
                    AnimationScaleType.None => 1.0f,
                    AnimationScaleType.XOnly => 1.0f,
                    _ => ReferenceScale
                };
                var baseSize = new Size(Util.Round(refScaleX * baseSpriteSize.Width), Util.Round(refScaleY * baseSpriteSize.Height));

                int newWidth = ScaleType switch
                {
                    AnimationScaleType.None => sprite.Width,
                    AnimationScaleType.YOnly => sprite.Width,
                    _ => Util.Round(baseSpriteSize.Width * scale)
                };
                int newHeight = ScaleType switch
                {
                    AnimationScaleType.None => sprite.Height,
                    AnimationScaleType.XOnly => sprite.Height,
                    _ => Util.Round(baseSpriteSize.Height * scale)
                };
                int newX = AnchorX switch
                {
                    HorizontalAnchor.Left => baseLocation.X - baseSize.Width / 2,
                    HorizontalAnchor.Right => baseLocation.X + baseSize.Width / 2 - newWidth,
                    _ => baseLocation.X - newWidth / 2
                };
                int newY = AnchorY switch
                {
                    VerticalAnchor.Top => baseLocation.Y - baseSize.Height / 2,
                    VerticalAnchor.Bottom => baseLocation.Y + baseSize.Height / 2 - newHeight,
                    _ => baseLocation.Y - newHeight / 2
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
            this.ticksPerFrame = Math.Max(1, ticksPerFrame);
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
            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frame * baseSpriteSize.Width * textureFactor, 0);
            Finished = true;
        }

        public bool Update(uint ticks)
        {
            if (ticksPerFrame == 0)
            {
                Finished = true;
                AnimationFinished?.Invoke();
                return !Finished;
            }

            if (ticks < startAnimationTicks)
            {
                sprite.Visible = false;
                return true;
            }
            else if (!sprite.Visible && wasVisible)
            {
                sprite.Visible = true;
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
                return !Finished;
            }

            float animationTime = frameIndices.Length * ticksPerFrame;
            float factor = elapsed / animationTime;
            baseSpriteLocation.X = startX + Util.Round((endX - startX) * factor);
            baseSpriteLocation.Y = startY + Util.Round((endY - startY) * factor);
            Scale = startScale + (endScale - startScale) * factor; // Note: scale will also set the new position
            sprite.TextureAtlasOffset = baseTextureCoords + new Position(frameIndices[frame] * sprite.TextureSize.Width * textureFactor, 0);
            AnimationUpdated?.Invoke(factor);

            return true;
        }

        public void SetDisplayLayer(byte displayLayer)
        {
            sprite.DisplayLayer = displayLayer;
        }
    }
}
