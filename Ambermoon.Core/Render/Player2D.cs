using Ambermoon.Data;
using System;

namespace Ambermoon.Render
{
    internal class Player2D : Character2D, IRenderPlayer
    {
        readonly Player player;
        readonly IMapManager mapManager;

        public Player2D(Game game, IRenderLayer layer, Player player, RenderMap2D map,
            ISpriteFactory spriteFactory, IGameData gameData, Position startPosition,
            IMapManager mapManager)
            : base(game, layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, gameData.PlayerAnimationInfo, map, startPosition, 7u)
        {
            this.player = player;
            this.mapManager = mapManager;
        }

        public bool Move(int x, int y, uint ticks, CharacterDirection? prevDirection = null,
            bool updateDirectionIfNotMoving = true) // x,y in tiles
        {
            if (player.MovementAbility == PlayerMovementAbility.NoMovement)
                return false;

            bool canMove = true;
            int newX = Position.X + x;
            int newY = Position.Y + y;
            var map = Map.Map;

            if (!map.IsWorldMap)
            {
                // Each map should have a border of 1 (walls)
                if (newX < 0 || newY < -1 || newX >= map.Width || newY >= map.Height - 1)
                    canMove = false;
            }
            else
            {
                while (newX < 0)
                    newX += map.Width;
                while (newY < 0)
                    newY += map.Height;
            }

            var tile = Map[(uint)newX, (uint)newY + 1];

            if (canMove)
            {
                switch (tile.Type)
                {
                    case Data.Map.TileType.Free:
                    case Data.Map.TileType.ChairUp:
                    case Data.Map.TileType.ChairRight:
                    case Data.Map.TileType.ChairDown:
                    case Data.Map.TileType.ChairLeft:
                    case Data.Map.TileType.Bed:
                    case Data.Map.TileType.Invisible:
                        canMove = true; // no movement was checked above
                        break;
                    case Data.Map.TileType.Obstacle:
                        canMove = player.MovementAbility >= PlayerMovementAbility.WitchBroom;
                        break;
                    case Data.Map.TileType.Water:
                        canMove = player.MovementAbility >= PlayerMovementAbility.Swimming;
                        break;
                    case Data.Map.TileType.Ocean:
                        canMove = player.MovementAbility >= PlayerMovementAbility.Sailing;
                        break;
                    case Data.Map.TileType.Mountain:
                        canMove = player.MovementAbility >= PlayerMovementAbility.Eagle;
                        break;
                    default:
                        canMove = false;
                        break;
                }
            }

            // TODO: display OUCH if moving against obstacles

            if (canMove)
            {
                var oldMap = map;
                int scrollX = 0;
                int scrollY = 0;
                prevDirection ??= Direction;
                var newDirection = CharacterDirection.Down;

                if (x > 0 && (map.IsWorldMap || (newX >= 6 && newX <= map.Width - 6)))
                    scrollX = 1;
                else if (x < 0 && (map.IsWorldMap || (newX <= map.Width - 7 && newX >= 5)))
                    scrollX = -1;

                if (y > 0 && (map.IsWorldMap || (newY >= 5 && newY <= map.Height - 5)))
                    scrollY = 1;
                else if (y < 0 && (map.IsWorldMap || (newY <= map.Height - 6 && newY >= 4)))
                    scrollY = -1;

                if (y > 0)
                    newDirection = CharacterDirection.Down;
                else if (y < 0)
                    newDirection = CharacterDirection.Up;
                else if (x > 0)
                    newDirection = CharacterDirection.Right;
                else if (x < 0)
                    newDirection = CharacterDirection.Left;

                Map.Scroll(scrollX, scrollY);

                if (oldMap == Map.Map)
                {
                    bool frameReset = NumFrames == 1 || newDirection != prevDirection;
                    var prevState = CurrentState;

                    MoveTo(oldMap, (uint)newX, (uint)newY, ticks, frameReset, null);
                    // We trigger with our lower half so add 1 to y
                    Map.TriggerEvents(this, MapEventTrigger.Move, (uint)newX, (uint)newY + 1, mapManager, ticks);

                    if (oldMap == Map.Map) // might have changed by map change events
                    {
                        if (!frameReset && CurrentState == prevState)
                            SetCurrentFrame((CurrentFrame + 1) % NumFrames);

                        var mapOffset = oldMap.MapOffset;
                        player.Position.X = mapOffset.X + Position.X - (int)Map.ScrollX;
                        player.Position.Y = mapOffset.Y + Position.Y - (int)Map.ScrollY;

                        Visible = tile.Type != Data.Map.TileType.Invisible;
                    }
                }
                else
                {
                    // adjust player position on map transition
                    var position = Map.GetCenterPosition();
                    MoveTo(Map.Map, (uint)position.X, (uint)position.Y, ticks, false, Direction);
                    
                    if (Map.Map.Type == MapType.Map2D)
                    {
                        var mapOffset = oldMap.MapOffset;
                        player.Position.X = mapOffset.X + Position.X - (int)Map.ScrollX;
                        player.Position.Y = mapOffset.Y + Position.Y - (int)Map.ScrollY;

                        // Note: For 3D maps the game/3D map will handle player position updating.
                    }
                }
            }
            else if (updateDirectionIfNotMoving)
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
                    MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, newDirection);
            }

            return canMove;
        }

        public override void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (Map.Map != map)
            {
                Visible = true; // reset visibility before changing map
            }

            base.MoveTo(map, x, y, ticks, frameReset, newDirection);
        }

        public override void Update(uint ticks)
        {
            // do not animate so don't call base.Update here
        }
    }
}
