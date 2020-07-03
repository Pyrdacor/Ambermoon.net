using Ambermoon.Data;

namespace Ambermoon.Render
{
    internal class Player2D : Character2D
    {
        readonly Player player;

        public Player2D(IRenderLayer layer, Player player, RenderMap map,
            ISpriteFactory spriteFactory, IGameData gameData, Position startPosition)
            : base(layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, gameData.PlayerAnimationInfo, map, startPosition)
        {
            this.player = player;
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
                while (newX >= map.Width)
                    newX -= map.Width;
                while (newY < 0)
                    newY += map.Height;
                while (newY >= map.Height)
                    newY -= map.Height;
            }

            var tile = map.Tiles[newX, newY];
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

                if (x > 0 && (map.IsWorldMap || (newX >= 6 && newX <= map.Width - 6)))
                    scrollX = 1;
                else if (x < 0 && (map.IsWorldMap || (newX <= map.Width - 7 && newX >= 5)))
                    scrollX = -1;

                if (y > 0 && (map.IsWorldMap || (newY >= 5 && newY <= map.Height - 5)))
                    scrollY = 1;
                else if (y < 0 && (map.IsWorldMap || (newY <= map.Height - 6 && newY >= 4)))
                    scrollY = -1;

                Map.Scroll(scrollX, scrollY);

                if (oldMap == Map.Map)
                {
                    MoveTo(oldMap, (uint)newX, (uint)newY, ticks);
                }
                else
                {
                    // TODO: adjust player position on map transition
                }
            }

            return canMove;
        }
    }
}
