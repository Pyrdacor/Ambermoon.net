using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon.Render
{
    internal class Player2D : Character2D, IRenderPlayer
    {
        readonly Game game;
        readonly Player player;
        readonly IMapManager mapManager;

        // TODO: gameData.PlayerAnimationInfo is only for Lyramion. Offsets need to be increased by World * 17 later.
        public Player2D(Game game, IRenderLayer layer, Player player, RenderMap2D map,
            ISpriteFactory spriteFactory, IGameData gameData, Position startPosition,
            IMapManager mapManager)
            : base(game, layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, game.GetPlayerAnimationInfo, map, startPosition,
                  game.GetPlayerPaletteIndex, game.GetPlayerDrawOffset)
        {
            this.game = game;
            this.player = player;
            this.mapManager = mapManager;
        }

        public bool Move(int x, int y, uint ticks, TravelType travelType,
            bool displayOuchIfBlocked = true, CharacterDirection? prevDirection = null,
            bool updateDirectionIfNotMoving = true) // x,y in tiles
        {
            if (player.MovementAbility == PlayerMovementAbility.NoMovement)
                return false;

            bool canMove = true;
            var map = Map.Map;
            int newX = Position.X + x;
            int newY = Position.Y + y;
            Map.Tile tile = null;

            if (!map.IsWorldMap)
            {
                // Don't leave the map.
                // Note that the player is 2 tiles tall in non-world maps
                // and the position is the upper tile so he is allowed to
                // move up to y = -1 and only down to y = map.Height - 1.
                if (newX < 0 || newY < -1 || newX >= map.Width || newY >= map.Height - 1)
                    canMove = false;
                else
                    tile = Map[(uint)newX, (uint)newY + 1];
            }
            else
            {
                while (newX < 0)
                    newX += map.Width;
                while (newY < 0)
                    newY += map.Height;

                tile = Map[(uint)newX, (uint)newY];
            }

            if (canMove)
            {
                var tileset = mapManager.GetTilesetForMap(map);
                canMove = tile.AllowMovement(tileset, travelType);

                if (!canMove && travelType == TravelType.Swim && tile.AllowMovement(tileset, TravelType.Walk))
                    canMove = true; // go on land
            }

            if (canMove)
            {
                var oldMap = map;
                int scrollX = 0;
                int scrollY = 0;
                prevDirection ??= Direction;
                var newDirection = CharacterDirection.Down;

                if (x > 0 && (map.IsWorldMap || (newX >= 6 && Map.ScrollX < Map.Map.Width - RenderMap2D.NUM_VISIBLE_TILES_X)))
                    scrollX = 1;
                else if (x < 0 && (map.IsWorldMap || (newX <= map.Width - 7 && Map.ScrollX > 0)))
                    scrollX = -1;

                if (y > 0 && (map.IsWorldMap || (newY >= 4 && Map.ScrollY < Map.Map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y)))
                    scrollY = 1;
                else if (y < 0 && (map.IsWorldMap || (newY <= map.Height - 7 && Map.ScrollY > 0)))
                    scrollY = -1;

                if (y > 0)
                    newDirection = CharacterDirection.Down;
                else if (y < 0)
                    newDirection = CharacterDirection.Up;
                else if (x > 0)
                    newDirection = CharacterDirection.Right;
                else if (x < 0)
                    newDirection = CharacterDirection.Left;

                player.Direction = newDirection;

                Map.Scroll(scrollX, scrollY);

                if (oldMap == Map.Map)
                {
                    bool frameReset = NumFrames == 1 || newDirection != prevDirection;
                    var prevState = CurrentState;

                    MoveTo(oldMap, (uint)newX, (uint)newY, ticks, frameReset, null);
                    // We trigger with our lower half so add 1 to y in non-world maps.
                    Map.TriggerEvents(this, MapEventTrigger.Move, (uint)newX,
                        (uint)newY + (oldMap.IsWorldMap ? 0u : 1u), mapManager, ticks);

                    if (oldMap == Map.Map) // might have changed by map change events
                    {
                        if (!frameReset && CurrentState == prevState)
                            SetCurrentFrame((CurrentFrame + 1) % NumFrames);

                        player.Position.X = Position.X;
                        player.Position.Y = Position.Y;

                        Visible = tile.Type != Data.Map.TileType.Invisible;

                        game.PlayerMoved(false);
                    }
                }
                else
                {
                    // adjust player position on map transition
                    var position = Map.GetCenterPosition();

                    MoveTo(Map.Map, (uint)position.X, (uint)position.Y, ticks, false, Direction);
                    Map.TriggerEvents(this, MapEventTrigger.Move, (uint)position.X,
                        (uint)position.Y + (Map.Map.IsWorldMap ? 0u : 1u), mapManager, ticks);

                    if (Map.Map.Type == MapType.Map2D)
                    {
                        player.Position.X = Position.X;
                        player.Position.Y = Position.Y;

                        // Note: For 3D maps the game/3D map will handle player position updating.

                        game.PlayerMoved(true);
                    }
                }
            }
            else
            {
                if (displayOuchIfBlocked)
                    game.DisplayOuch();

                if (updateDirectionIfNotMoving)
                {
                    // If not able to move, the direction should be adjusted
                    var newDirection = Direction;

                    if (y > 0)
                        newDirection = CharacterDirection.Down;
                    else if (y < 0)
                        newDirection = CharacterDirection.Up;
                    else if (x > 0)
                        newDirection = CharacterDirection.Right;
                    else if (x < 0)
                        newDirection = CharacterDirection.Left;

                    if (newDirection != Direction)
                    {
                        MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, newDirection);
                        player.Direction = newDirection;
                        UpdateAppearance(game.CurrentTicks);
                    }
                }
            }

            return canMove;
        }

        public void UpdateAppearance(uint ticks)
        {
            MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, null);
        }

        public override void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (Map.Map != map)
                Visible = true; // reset visibility before changing map

            base.MoveTo(map, x, y, ticks, frameReset, newDirection);
        }

        public override void Update(uint ticks)
        {
            // do not animate so don't call base.Update here
        }
    }
}
