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

        readonly Game game;
        readonly ITextureAtlas textureAtlas;
        readonly IAnimatedSprite sprite;
        readonly Func<Character2DAnimationInfo> animationInfoProvider;
        readonly Func<uint> paletteIndexProvider;
        readonly Func<Position> drawOffsetProvider;
        Character2DAnimationInfo CurrentAnimationInfo => animationInfoProvider();
        public uint CurrentBaseFrameIndex { get; private set; }
        public uint CurrentFrameIndex { get; private set; }
        public uint CurrentFrame => sprite.CurrentFrame;
        uint lastFrameReset = 0u;
        int baselineOffset = 0;
        public int BaselineOffset
        {
            get => baselineOffset;
            set
            {
                if (baselineOffset == value)
                    return;

                baselineOffset = value;
                UpdateBaseline();
            }
        }
        public CharacterDirection Direction { get; private set; } = CharacterDirection.Down;
        public RenderMap2D Map { get; private set; } // Note: No character will appear on world maps so the map is always a non-world map (only exception is the player)
        public Position Position { get; } // in tiles
        public Rect DisplayArea => new Rect(sprite.X, sprite.Y, sprite.Width, sprite.Height);
        public bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = value;
        }
        public State CurrentState { get; private set; }
        public uint NumFrames => Math.Max(1, CurrentState switch
        {
            State.Stand => CurrentAnimationInfo.NumStandFrames,
            State.Sit => CurrentAnimationInfo.NumSitFrames,
            State.Sleep => CurrentAnimationInfo.NumSleepFrames,
            _ => throw new ArgumentOutOfRangeException("Invalid character state")
        });

        public Character2D(Game game, IRenderLayer layer, ITextureAtlas textureAtlas, ISpriteFactory spriteFactory,
            Func<Character2DAnimationInfo> animationInfoProvider, RenderMap2D map, Position startPosition,
            Func<uint> paletteIndexProvider, Func<Position> drawOffsetProvider)
        {
            this.game = game;
            Map = map;
            this.textureAtlas = textureAtlas;
            this.animationInfoProvider = animationInfoProvider;
            this.paletteIndexProvider = paletteIndexProvider;
            this.drawOffsetProvider = drawOffsetProvider;
            var currentAnimationInfo = CurrentAnimationInfo;
            CurrentBaseFrameIndex = CurrentFrameIndex = currentAnimationInfo.StandFrameIndex;
            var textureOffset = textureAtlas.GetOffset(CurrentFrameIndex);
            sprite = spriteFactory.CreateAnimated(currentAnimationInfo.FrameWidth, currentAnimationInfo.FrameHeight,
                textureAtlas.Texture.Width, currentAnimationInfo.NumStandFrames);
            sprite.TextureAtlasOffset = textureOffset;
            sprite.Layer = layer;
            var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
            sprite.X = Global.Map2DViewX + (startPosition.X - (int)map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
            sprite.Y = Global.Map2DViewY + (startPosition.Y - (int)map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y;
            sprite.PaletteIndex = (byte)paletteIndexProvider();
            sprite.ClipArea = Game.Map2DViewArea;
            UpdateBaseline();
            Position = startPosition;
        }

        public virtual void Destroy()
        {
            sprite?.Delete();
        }

        void UpdateBaseline()
        {
            var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
            sprite.BaseLineOffset = baselineOffset + 1 - drawOffset.Y +
                Math.Max(0, RenderMap2D.TILE_HEIGHT - CurrentAnimationInfo.FrameHeight % RenderMap2D.TILE_HEIGHT);
        }

        public virtual void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (map != Map.Map)
            {
                if (newDirection == null)
                    throw new AmbermoonException(ExceptionScope.Application, "Direction must be given when changing maps.");

                if (map.Type == MapType.Map2D)
                {
                    Map.SetMap(map,
                        (uint)Util.Limit(0, (int)x - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                        (uint)Util.Limit(0, (int)y - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y));
                    Direction = newDirection.Value;
                }
                else
                {
                    game.Start3D(map, x, y, newDirection.Value, false);
                    return;
                }
            }
            else if (newDirection == null)
            {
                // Only adjust direction when not changing the map.

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
            }
            else
            {
                Direction = newDirection.Value;
            }

            if (x == uint.MaxValue || y == uint.MaxValue)
            {
                Position.X = (int)x;
                Position.Y = (int)y;
                sprite.Visible = false;
            }
            else
            {
                var animationInfo = CurrentAnimationInfo;
                var tileType = Map[x, y + 1].Type;
                sprite.Resize(animationInfo.FrameWidth, animationInfo.FrameHeight);
                CurrentState = animationInfo.IgnoreTileType ? State.Stand : tileType switch
                {
                    Data.Map.TileType.ChairUp => State.Sit,
                    Data.Map.TileType.ChairRight => State.Sit,
                    Data.Map.TileType.ChairDown => State.Sit,
                    Data.Map.TileType.ChairLeft => State.Sit,
                    Data.Map.TileType.Bed => State.Sleep,
                    _ => State.Stand
                };
                sprite.NumFrames = NumFrames;
                CurrentBaseFrameIndex = animationInfo.IgnoreTileType ? animationInfo.StandFrameIndex : tileType switch
                {
                    Data.Map.TileType.ChairUp => animationInfo.SitFrameIndex,
                    Data.Map.TileType.ChairRight => animationInfo.SitFrameIndex + 1,
                    Data.Map.TileType.ChairDown => animationInfo.SitFrameIndex + 2,
                    Data.Map.TileType.ChairLeft => animationInfo.SitFrameIndex + 3,
                    Data.Map.TileType.Bed => animationInfo.SleepFrameIndex,
                    _ => animationInfo.StandFrameIndex
                };
                if (!animationInfo.NoDirections && CurrentBaseFrameIndex == animationInfo.StandFrameIndex)
                    CurrentBaseFrameIndex += (uint)Direction * sprite.NumFrames;
                CurrentFrameIndex = CurrentBaseFrameIndex;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(CurrentFrameIndex);
                if (frameReset)
                {
                    sprite.CurrentFrame = 0;
                    lastFrameReset = ticks;
                }
                else
                    sprite.CurrentFrame = sprite.CurrentFrame; // this may correct the value if NumFrames has changed
                Position.X = (int)x;
                Position.Y = (int)y;
                var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
                sprite.X = Global.Map2DViewX + (Position.X - (int)Map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                sprite.Y = Global.Map2DViewY + (Position.Y - (int)Map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y;
                sprite.PaletteIndex = (byte)paletteIndexProvider();
                sprite.Visible = Game.Map2DViewArea.IntersectsWith(DisplayArea);
                UpdateBaseline();
            }
        }

        public virtual void Update(uint ticks, Time gameTime)
        {
            uint elapsedTicks = ticks - lastFrameReset;
            sprite.CurrentFrame = elapsedTicks / CurrentAnimationInfo.TicksPerFrame; // this will take care of modulo frame count
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }

        public void SetCurrentFrame(uint frameIndex)
        {
            sprite.CurrentFrame = frameIndex; // this will take care of modulo frame count
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }
    }
}
