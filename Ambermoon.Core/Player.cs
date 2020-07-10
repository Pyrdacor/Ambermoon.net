namespace Ambermoon
{
    internal class Player
    {
        /// <summary>
        /// On world maps this is the total coordinate.
        /// On all other 2D or 3D maps this is the map coordinate.
        /// The position is given in tiles.
        /// </summary>
        public Position Position { get; } = new Position(0, 0);
        public CharacterDirection Direction { get; set; } = CharacterDirection.Down;
        public PlayerMovementAbility MovementAbility { get; set; } = PlayerMovementAbility.NoMovement;
    }
}
