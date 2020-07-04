using Ambermoon.Data;
using System;

namespace Ambermoon.Render
{
    // A 2D character like the player, NPCs or enemies
    // is a movable sprite which supports animation.
    // On each movement the animation frame changes.
    // Some characters may have also animations while
    // not moving. Characters will sit if they move onto
    // a chair and will sleep if they move onto a bed.
    internal class Character2D
    {
        public enum State
        {
            Stand,
            Sit,
            Sleep
        }

        readonly ITextureAtlas textureAtlas;
        readonly IAnimatedSprite sprite;
        readonly Character2DAnimationInfo animationInfo;
        public uint CurrentBaseFrameIndex { get; private set; }
        public uint CurrentFrameIndex { get; private set; }
        public uint CurrentFrame => sprite.CurrentFrame;
        uint lastFrameReset = 0u;
        public CharacterDirection Direction { get; private set; } = CharacterDirection.Down;
        public RenderMap2D Map { get; private set; } // Note: No character will appear on world maps so the map is always a non-world map (exception is the player)
        public Position Position { get; } // in Tiles
        public bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = value;
        }
        public State CurrentState { get; private set; }
        public uint NumFrames => CurrentState switch
        {
            State.Stand => animationInfo.NumStandFrames,
            State.Sit => animationInfo.NumSitFrames,
            State.Sleep => animationInfo.NumSleepFrames,
            _ => throw new ArgumentOutOfRangeException("Invalid character state")
        };

        public Character2D(IRenderLayer layer, ITextureAtlas textureAtlas, ISpriteFactory spriteFactory,
            Character2DAnimationInfo animationInfo, RenderMap2D map, Position startPosition)
        {
            this.textureAtlas = textureAtlas;
            this.animationInfo = animationInfo;
            CurrentBaseFrameIndex = CurrentFrameIndex = animationInfo.StandFrameIndex;
            var textureOffset = textureAtlas.GetOffset(CurrentFrameIndex);
            sprite = spriteFactory.CreateAnimated(animationInfo.FrameWidth, animationInfo.FrameHeight,
                textureOffset.X, textureOffset.Y, textureAtlas.Texture.Width, animationInfo.NumStandFrames);
            sprite.Layer = layer;
            sprite.X = Global.MapViewX + (startPosition.X - (int)map.ScrollX) * RenderMap2D.TILE_WIDTH;
            sprite.Y = Global.MapViewY + (startPosition.Y - (int)map.ScrollY) * RenderMap2D.TILE_HEIGHT;
            sprite.BaseLineOffset = 1;
            Map = map;
            Position = startPosition;
        }

        public void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset)
        {
            if (map != Map.Map)
            {
                Map.SetMap(map, (uint)Math.Max(0, (int)x - 6), (uint)Math.Max(0, (int)y - 5));
            }

            // Note: Whenever y changes the front/back frame is used.
            // Only for pure x movements the side frames are used.
            if (y < Position.Y)
            {
                // Move back (look up)
                Direction = CharacterDirection.Up;
            }
            else if (y > Position.Y)
            {
                // Move front (look down)
                Direction = CharacterDirection.Down;
            }
            else if (x < Position.X)
            {
                // Move purely left
                Direction = CharacterDirection.Left;
            }
            else if (x > Position.X)
            {
                // Move purely right
                Direction = CharacterDirection.Right;
            }

            var tileType = Map.Map.Tiles[x, y].Type;
            CurrentState = tileType switch
            {
                Data.Map.TileType.Chair => State.Sit,
                Data.Map.TileType.Bed => State.Sleep,
                _ => State.Stand
            };
            sprite.NumFrames = NumFrames;
            CurrentBaseFrameIndex = tileType switch
            {
                Data.Map.TileType.Chair => animationInfo.SitFrameIndex,
                Data.Map.TileType.Bed => animationInfo.SleepFrameIndex,
                _ => animationInfo.StandFrameIndex
            } + (uint)Direction * sprite.NumFrames;
            CurrentFrameIndex = CurrentBaseFrameIndex;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(CurrentFrameIndex);
            if (frameReset)
                sprite.CurrentFrame = 0;
            else
                sprite.CurrentFrame = sprite.CurrentFrame; // this may correct the value if NumFrames has changed
            lastFrameReset = ticks;
            Position.X = (int)x;
            Position.Y = (int)y;
            sprite.X = Global.MapViewX + (Position.X - (int)Map.ScrollX) * RenderMap2D.TILE_WIDTH;
            sprite.Y = Global.MapViewY + (Position.Y - (int)Map.ScrollY) * RenderMap2D.TILE_HEIGHT;
        }

        public virtual void Update(uint ticks)
        {
            uint elapsedTicks = ticks - lastFrameReset;
            sprite.CurrentFrame = elapsedTicks / animationInfo.TicksPerFrame; // this will take care of modulo frame count
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }

        public void SetCurrentFrame(uint frameIndex)
        {
            sprite.CurrentFrame = frameIndex; // this will take care of modulo frame count
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }
    }
}
