using Ambermoon.Data.GameDataRepository.Util;

namespace Ambermoon.Data.GameDataRepository.Enumerations
{
    [Flags]
    public enum Wall3DFlags : uint
    {
        /// <summary>
        /// If this is active the tile will block sight.
        /// Monsters can't see through it.
        /// </summary>
        BlockSight = 0x00000002,
        /// <summary>
        /// Normally walls use color index 0 for black.
        /// If this is set, color index 0 is instead
        /// full transparent.
        /// </summary>
        Transparency = 0x00000008,
        /// <summary>
        /// Blocks all movement. Monsters and the player can't
        /// enter the tile, no matter the transportation they use.
        ///
        /// This is a general switch and will override all other
        /// movement allow bits below if it is set.
        /// </summary>
        BlockAllMovement = 0x00000080,
        /// <summary>
        /// Allows the player to enter the tile and also
        /// all map characters with collision class 1.
        /// </summary>
        AllowMovement1 = 0x00000100,
        /// <summary>
        /// Allows all map characters with collision class
        /// 2 to enter the tile.
        /// </summary>
        AllowMovement2 = 0x00000200,
        /// <summary>
        /// Allows all map characters with collision class
        /// 3 to enter the tile.
        /// </summary>
        AllowMovement3 = 0x00000400,
        /// <summary>
        /// Allows all map characters with collision class
        /// 4 to enter the tile.
        /// </summary>
        AllowMovement4 = 0x00000800,
        /// <summary>
        /// Allows all map characters with collision class
        /// 5 to enter the tile.
        /// </summary>
        AllowMovement5 = 0x00001000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 6 to enter the tile.
        /// </summary>
        AllowMovement6 = 0x00002000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 7 to enter the tile.
        /// </summary>
        AllowMovement7 = 0x00004000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 8 to enter the tile.
        /// </summary>
        AllowMovement8 = 0x00008000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 9 to enter the tile.
        /// </summary>
        AllowMovement9 = 0x00010000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 10 to enter the tile.
        /// </summary>
        AllowMovement10 = 0x00020000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 11 to enter the tile.
        /// </summary>
        AllowMovement11 = 0x00040000,
        /// <summary>
        /// Allows all map characters with collision class
        /// 12 to enter the tile.
        /// </summary>
        AllowMovement12 = 0x00080000,
        /// <summary>
        /// Mask for the combat background index.
        /// </summary>
        CombatBackgroundMask = 0xf0000000
    }
}
