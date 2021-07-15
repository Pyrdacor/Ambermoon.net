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
        readonly ISpriteFactory spriteFactory;
        readonly ITextureAtlas textureAtlas;
        IAnimatedSprite topSprite; // for non-world maps the upper half is drawn separatly
        readonly IAnimatedSprite sprite;
        readonly Func<Character2DAnimationInfo> animationInfoProvider;
        readonly Func<uint> paletteIndexProvider;
        readonly Func<Position> drawOffsetProvider;
        bool active = true;
        bool visible = true;
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
        public Rect DisplayArea => new Rect(topSprite?.X ?? sprite.X, topSprite?.Y ?? sprite.Y,
            Math.Max(topSprite?.Width ?? 0, sprite.Width), (topSprite?.Height ?? 0) + sprite.Height);
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;

                sprite.Visible = visible && active;

                if (topSprite != null)
                    topSprite.Visible = sprite.Visible;
            }
        }
        public bool Active
        {
            get => active;
            set
            {
                active = value;

                sprite.Visible = visible && active;

                if (topSprite != null)
                    topSprite.Visible = sprite.Visible;
            }
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
            this.spriteFactory = spriteFactory;
            Map = map;
            this.textureAtlas = textureAtlas;
            this.animationInfoProvider = animationInfoProvider;
            this.paletteIndexProvider = paletteIndexProvider;
            this.drawOffsetProvider = drawOffsetProvider;
            var currentAnimationInfo = CurrentAnimationInfo;
            CurrentBaseFrameIndex = CurrentFrameIndex = currentAnimationInfo.StandFrameIndex;
            var textureOffset = textureAtlas.GetOffset(CurrentFrameIndex);
            var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
            if (currentAnimationInfo.UseTopSprite)
            {
                sprite = spriteFactory.CreateAnimated(currentAnimationInfo.FrameWidth, Math.Min(currentAnimationInfo.FrameHeight, RenderMap2D.TILE_HEIGHT),
                    textureAtlas.Texture.Width, currentAnimationInfo.NumStandFrames);
                topSprite = spriteFactory.CreateAnimated(currentAnimationInfo.FrameWidth, Math.Max(0, currentAnimationInfo.FrameHeight - RenderMap2D.TILE_HEIGHT),
                    textureAtlas.Texture.Width, currentAnimationInfo.NumStandFrames);
                topSprite.TextureAtlasOffset = textureOffset;
                sprite.TextureAtlasOffset = textureOffset + new Position(0, currentAnimationInfo.FrameHeight - RenderMap2D.TILE_HEIGHT);
                topSprite.Layer = layer;
                topSprite.PaletteIndex = (byte)paletteIndexProvider();
                topSprite.ClipArea = Game.Map2DViewArea;
                topSprite.X = Global.Map2DViewX + (startPosition.X - (int)map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                topSprite.Y = Global.Map2DViewY + (startPosition.Y - (int)map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y;
                sprite.X = Global.Map2DViewX + (startPosition.X - (int)map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                sprite.Y = Global.Map2DViewY + (startPosition.Y - (int)map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y +
                    currentAnimationInfo.FrameHeight - RenderMap2D.TILE_HEIGHT;
            }
            else
            {
                sprite = spriteFactory.CreateAnimated(currentAnimationInfo.FrameWidth, currentAnimationInfo.FrameHeight,
                    textureAtlas.Texture.Width, currentAnimationInfo.NumStandFrames);
                sprite.TextureAtlasOffset = textureOffset;
                sprite.X = Global.Map2DViewX + (startPosition.X - (int)map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                sprite.Y = Global.Map2DViewY + (startPosition.Y - (int)map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y;
            }            
            sprite.Layer = layer;
            sprite.PaletteIndex = (byte)paletteIndexProvider();
            sprite.ClipArea = Game.Map2DViewArea;
            UpdateBaseline();
            Position = startPosition;
        }

        internal void RecheckTopSprite()
        {
            var currentAnimationInfo = CurrentAnimationInfo;

            if (currentAnimationInfo.UseTopSprite && currentAnimationInfo.FrameHeight > RenderMap2D.TILE_HEIGHT)
            {
                if (topSprite == null)
                {
                    sprite.Resize(currentAnimationInfo.FrameWidth, Math.Min(currentAnimationInfo.FrameHeight, RenderMap2D.TILE_HEIGHT));
                    topSprite = spriteFactory.CreateAnimated(currentAnimationInfo.FrameWidth, Math.Max(0, currentAnimationInfo.FrameHeight - RenderMap2D.TILE_HEIGHT),
                        textureAtlas.Texture.Width, currentAnimationInfo.NumStandFrames);
                    topSprite.TextureAtlasOffset = sprite.TextureAtlasOffset;
                    sprite.TextureAtlasOffset += new Position(0, topSprite.Height);
                    topSprite.Layer = sprite.Layer;
                    topSprite.PaletteIndex = sprite.PaletteIndex;
                    topSprite.ClipArea = Game.Map2DViewArea;
                    topSprite.X = sprite.X;
                    topSprite.Y = sprite.Y;
                    sprite.Y += topSprite.Height;
                }                
            }
            else
            {
                if (topSprite != null)
                {
                    sprite.TextureAtlasOffset = topSprite.TextureAtlasOffset;
                    sprite.Resize(currentAnimationInfo.FrameWidth, currentAnimationInfo.FrameHeight);
                    sprite.X = topSprite.X;
                    sprite.Y = topSprite.Y;
                    topSprite.Delete();
                    topSprite = null;
                }
            }
        }

        public virtual void Destroy()
        {
            sprite?.Delete();
            topSprite?.Delete();
        }

        void UpdateBaseline()
        {
            if (this is Player2D && baselineOffset == Game.MaxBaseLine)
            {
                sprite.BaseLineOffset = Game.MaxBaseLine;
                if (topSprite != null)
                    topSprite.BaseLineOffset = Game.MaxBaseLine;
            }
            else
            {
                var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
                sprite.BaseLineOffset = baselineOffset + 1 - drawOffset.Y + (topSprite == null ? CurrentAnimationInfo.FrameHeight :
                    Math.Max(0, RenderMap2D.TILE_HEIGHT - CurrentAnimationInfo.FrameHeight % RenderMap2D.TILE_HEIGHT) - RenderMap2D.TILE_HEIGHT);
                if (topSprite != null)
                    topSprite.BaseLineOffset = baselineOffset + 1 - drawOffset.Y + CurrentAnimationInfo.FrameHeight;
            }
        }

        public void SetDirection(CharacterDirection direction, uint ticks)
        {
            MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, direction);
        }

        public virtual void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (newDirection == CharacterDirection.Random)
                newDirection = (CharacterDirection)game.RandomInt(0, 3);

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

                    RecheckTopSprite();
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

            bool moved = true;

            if (x == uint.MaxValue || y == uint.MaxValue)
            {
                Position.X = (int)x;
                Position.Y = (int)y;
                sprite.Visible = false;
                if (topSprite != null)
                    topSprite.Visible = false;
            }
            else
            {
                var animationInfo = CurrentAnimationInfo;
                var tileType = Map[x, y].Type;
                if (topSprite == null)
                    sprite.Resize(animationInfo.FrameWidth, animationInfo.FrameHeight);
                else
                {
                    topSprite.Resize(animationInfo.FrameWidth, animationInfo.FrameHeight - RenderMap2D.TILE_HEIGHT);
                    sprite.Resize(animationInfo.FrameWidth, RenderMap2D.TILE_HEIGHT);
                }
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
                if (topSprite != null)
                    topSprite.NumFrames = NumFrames;
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
                if (topSprite == null)
                    sprite.TextureAtlasOffset = textureAtlas.GetOffset(CurrentFrameIndex);
                else
                {
                    topSprite.TextureAtlasOffset = textureAtlas.GetOffset(CurrentFrameIndex);
                    sprite.TextureAtlasOffset = topSprite.TextureAtlasOffset + new Position(0, topSprite.Height);
                }
                if (frameReset)
                {
                    sprite.CurrentFrame = 0;
                    if (topSprite != null)
                        topSprite.CurrentFrame = 0;
                    lastFrameReset = ticks;
                }
                else
                {
                    sprite.CurrentFrame = sprite.CurrentFrame; // this may correct the value if NumFrames has changed
                    if (topSprite != null)
                        topSprite.CurrentFrame = sprite.CurrentFrame;
                }
                if (Position.X == x && Position.Y == y)
                    moved = false;
                else
                {
                    Position.X = (int)x;
                    Position.Y = (int)y;
                }
                var drawOffset = drawOffsetProvider?.Invoke() ?? new Position();
                sprite.PaletteIndex = (byte)paletteIndexProvider();
                if (topSprite == null)
                {
                    sprite.X = Global.Map2DViewX + (Position.X - (int)Map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                    sprite.Y = Global.Map2DViewY + (Position.Y - (int)Map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y;
                }
                else
                {
                    topSprite.X = Global.Map2DViewX + (Position.X - (int)Map.ScrollX) * RenderMap2D.TILE_WIDTH + drawOffset.X;
                    topSprite.Y = Global.Map2DViewY + (Position.Y - (int)Map.ScrollY) * RenderMap2D.TILE_HEIGHT + drawOffset.Y +
                        RenderMap2D.TILE_HEIGHT - animationInfo.FrameHeight;
                    sprite.X = topSprite.X;
                    sprite.Y = topSprite.Y + topSprite.Height;
                    topSprite.PaletteIndex = sprite.PaletteIndex;
                    topSprite.Visible = Game.Map2DViewArea.IntersectsWith(DisplayArea);
                }
                sprite.Visible = Game.Map2DViewArea.IntersectsWith(DisplayArea);
                if (sprite.Visible && tileType == Data.Map.TileType.Invisible)
                    sprite.Visible = false;
                if (topSprite != null && topSprite.Visible && tileType == Data.Map.TileType.Invisible)
                    topSprite.Visible = false;
                UpdateBaseline();
            }

            if (moved)
            {
                if (this is Player2D)
                    Map.CheckIfMonstersSeePlayer(x, y);
                else if (this is MapCharacter2D monster && monster.IsMonster)
                    Map.CheckIfMonsterSeesPlayer(monster, sprite.Visible);
            }
        }

        public virtual void Update(uint ticks, ITime gameTime, bool allowInstantMovement = false,
            Position lastPlayerPosition = null)
        {
            uint elapsedTicks = ticks - lastFrameReset;
            sprite.CurrentFrame = elapsedTicks / CurrentAnimationInfo.TicksPerFrame; // this will take care of modulo frame count
            if (topSprite != null)
                topSprite.CurrentFrame = sprite.CurrentFrame;
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }

        public void SetCurrentFrame(uint frameIndex)
        {
            sprite.CurrentFrame = frameIndex; // this will take care of modulo frame count
            if (topSprite != null)
                topSprite.CurrentFrame = frameIndex;
            CurrentFrameIndex = CurrentBaseFrameIndex + sprite.CurrentFrame;
        }
    }
}
