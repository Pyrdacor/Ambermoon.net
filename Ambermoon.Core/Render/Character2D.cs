using Ambermoon.Data;

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
        readonly ITextureAtlas textureAtlas;
        readonly IAnimatedSprite sprite;
        readonly Character2DAnimationInfo animationInfo;
        uint currentFrameIndex;
        uint lastFrameReset = 0u;
        CharacterDirection direction = CharacterDirection.Down;

        public Map Map { get; } // Note: No character will appear on world maps so the map is always a non-world map (exception is the player)
        public Position Position { get; } // in Tiles
        public bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = value;
        }

        public Character2D(IRenderLayer layer, ITextureAtlas textureAtlas, ISpriteFactory spriteFactory,
            Character2DAnimationInfo animationInfo, Map map, Position startPosition)
        {
            this.textureAtlas = textureAtlas;
            this.animationInfo = animationInfo;
            currentFrameIndex = animationInfo.StandFrameIndex;
            var textureOffset = textureAtlas.GetOffset(currentFrameIndex);
            sprite = spriteFactory.CreateAnimated(animationInfo.FrameWidth, animationInfo.FrameHeight,
                textureOffset.X, textureOffset.Y, textureAtlas.Texture.Width, animationInfo.NumStandFrames);
            sprite.Layer = layer;
            sprite.X = Global.MapViewX + startPosition.X * RenderMap.TILE_WIDTH; // TODO: scroll
            sprite.Y = Global.MapViewY + startPosition.Y * RenderMap.TILE_HEIGHT; // TODO: scroll
            Map = map;
            Position = startPosition;
        }

        public void MoveTo(Map map, uint x, uint y, uint ticks)
        {
            // Note: Whenever y changes the front/back frame is used.
            // Only for pure x movements the side frames are used.
            if (y < Position.Y)
            {
                // Move back (look up)
                direction = CharacterDirection.Up;
            }
            else if (y > Position.Y)
            {
                // Move front (look down)
                direction = CharacterDirection.Down;
            }
            else if (x < Position.X)
            {
                // Move purely left
                direction = CharacterDirection.Left;
            }
            else if (x > Position.Y)
            {
                // Move purely right
                direction = CharacterDirection.Right;
            }

            var tileType = map.Tiles[x, y].Type;
            sprite.NumFrames = tileType switch
            {
                Map.TileType.Chair => animationInfo.NumSitFrames,
                Map.TileType.Bed => animationInfo.NumSleepFrames,
                _ => animationInfo.NumStandFrames
            };
            currentFrameIndex = tileType switch
            {
                Map.TileType.Chair => animationInfo.SitFrameIndex,
                Map.TileType.Bed => animationInfo.SleepFrameIndex,
                _ => animationInfo.StandFrameIndex
            } + (uint)direction * sprite.NumFrames;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(currentFrameIndex);
            sprite.CurrentFrame = 0;
            lastFrameReset = ticks;
            Position.X = (int)x;
            Position.Y = (int)y;
        }

        public void Update(uint ticks)
        {
            uint elapsedTicks = ticks - lastFrameReset;
            currentFrameIndex = sprite.CurrentFrame = elapsedTicks / animationInfo.TicksPerFrame;
        }
    }
}
