using Ambermoon.Data;
using System;

namespace Ambermoon.Render
{
    internal class Player2D : Character2D, IRenderPlayer
    {
        readonly Player player;
        readonly IMapManager mapManager;

        public Player2D(IRenderLayer layer, Player player, RenderMap2D map,
            ISpriteFactory spriteFactory, IGameData gameData, Position startPosition,
            IMapManager mapManager)
            : base(layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, gameData.PlayerAnimationInfo, map, startPosition, 7u)
        {
            this.player = player;
            this.mapManager = mapManager;
        }

        public bool Move(int x, int y, uint ticks) // in Tiles
        {
            if (player.MovementAbility == PlayerMovementAbility.NoMovement)
                return false;

            int newX = Position.X + x;
            int newY = Position.Y + y;
            var map = Map.Map;

            if (!map.IsWorldMap)
            {
                // Each map should have a border of 1 (walls)
                if (newX < 1 || newY < 1 || newX >= map.Width - 1 || newY >= map.Height - 1)
                    return false;
            }
            else
            {
                while (newX < 0)
                    newX += map.Width;
                while (newY < 0)
                    newY += map.Height;

                if (!Map.Map.IsWorldMap)
                {
                    while (newX >= map.Width)
                        newX -= map.Width;
                    while (newY >= map.Height)
                        newY -= map.Height;
                }
            }

            var tile = Map[(uint)newX, (uint)newY];
            bool canMove;

            switch (tile.Type)
            {
                case Data.Map.TileType.Free:
                case Data.Map.TileType.Chair:
                case Data.Map.TileType.Bed:
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

            if (canMove)
            {
                var oldMap = map;
                int scrollX = 0;
                int scrollY = 0;
                var prevDirection = Direction;
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

                    MoveTo(oldMap, (uint)newX, (uint)newY, ticks, frameReset, false);
                    // We trigger with our lower half so add 1 to y
                    Map.TriggerEvents(this, MapEventTrigger.Move, (uint)newX, (uint)newY + 1, mapManager, ticks);

                    if (!frameReset && CurrentState == prevState)
                        SetCurrentFrame((CurrentFrame + 1) % NumFrames);
                }
                else
                {
                    // adjust player position on map transition
                    var position = Map.GetCenterPosition();
                    MoveTo(Map.Map, (uint)position.X, (uint)position.Y, ticks, false, true);
                }
            }

            return canMove;
        }

        public override void Update(uint ticks)
        {
            // do not animate so don't call base.Update here
        }
    }
}
