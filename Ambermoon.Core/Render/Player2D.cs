using Ambermoon.Data;

namespace Ambermoon.Render
{
    internal class Player2D : Character2D
    {
        readonly RenderMap renderMap;
        readonly Player player;

        public Player2D(IRenderLayer layer, Player player, RenderMap map,
            ISpriteFactory spriteFactory, IGameData gameData, Position startPosition)
            : base(layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, gameData.PlayerAnimationInfo, map.Map, startPosition)
        {
            this.player = player;
            renderMap = map;
        }

        public bool Move(int x, int y) // in Tiles
        {
            if (player.MovementAbility == PlayerMovementAbility.NoMovement)
                return false;

            int newX = Position.X + x;
            int newY = Position.Y + y;

            if (!Map.IsWorldMap)
            {
                // Each map should have a border of 1 (walls)
                if (newX < 1 || newY < 1 || newX >= Map.Width - 1 || newY >= Map.Height - 1)
                    return false;
            }
            else
            {
                while (newX < 0)
                    newX += Map.Width;
                while (newX >= Map.Width)
                    newX -= Map.Width;
                while (newY < 0)
                    newY += Map.Height;
                while (newY >= Map.Height)
                    newY -= Map.Height;
            }

            var tile = Map.Tiles[newX, newY];
            bool canMove;

            switch (tile.Type)
            {
                case Map.TileType.Free:
                case Map.TileType.Chair:
                case Map.TileType.Bed:
                    canMove = true; // no movement was checked above
                    break;
                case Map.TileType.Obstacle:
                    canMove = player.MovementAbility >= PlayerMovementAbility.WitchBroom;
                    break;
                case Map.TileType.Water:
                    canMove = player.MovementAbility >= PlayerMovementAbility.Swimming;
                    break;
                case Map.TileType.Ocean:
                    canMove = player.MovementAbility >= PlayerMovementAbility.Sailing;
                    break;
                case Map.TileType.Mountain:
                    canMove = player.MovementAbility >= PlayerMovementAbility.Eagle;
                    break;
                default:
                    canMove = false;
                    break;
            }

            if (canMove)
            {
                Position.X = newX;
                Position.Y = newY;
                int scrollX = 0;
                int scrollY = 0;

                if (x > 0 && (Map.IsWorldMap || newX == 6))
                    scrollX = 1;
                else if (x < 0 && (Map.IsWorldMap || newX == Map.Width - 7))
                    scrollX = -1;

                if (y > 0 && (Map.IsWorldMap || newY == 5))
                    scrollY = 1;
                else if (y < 0 && (Map.IsWorldMap || newY == Map.Height - 6))
                    scrollY = -1;

                renderMap.Scroll(scrollX, scrollY);
            }

            return canMove;
        }
    }
}
