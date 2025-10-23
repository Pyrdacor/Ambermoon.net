namespace Ambermoon.Data.GameDataRepository.Enumerations;

[Flags]
internal enum Object3DFlags : uint
{
    /// <summary>
    /// Normally animations are cyclic. So if the last frame
    /// is reached, it starts again at the first frame.
    /// 
    /// If this is active the animation instead will decrease
    /// the frames one by one after reaching the last frame.
    /// So it will be a forth and back frame iteration.
    /// 
    /// 0 -> 1 -> 2 -> 1 -> 0 -> 1 -> 2 -> 1 -> ...
    /// 
    /// Instead of:
    /// 
    /// 0 -> 1 -> 2 -> 0 -> 1 -> 2 -> 0 -> 1 -> ...
    /// </summary>
    WaveAnimation = 0x00000001,
    /// <summary>
    /// If this is active the tile will block sight.
    /// Monsters can't see through it.
    /// </summary>
    BlockSight = 0x00000002,
    /// <summary>
    /// Normally objects are rendered as 2D billboards
    /// which always face the player. If this is set,
    /// the object will be rendered as a layer above
    /// the ground. This is used for holes in the ground
    /// or in the ceiling, tables, portals, carpets, etc.
    /// </summary>
    Floor = 0x00000008,
    /// <summary>
    /// Normally animations are started at map entering
    /// and will just loop continuously. If this is set,
    /// the animation is started at a random frame and
    /// after a full cycle/wave it will stop and wait
    /// for the next random start.
    /// </summary>
    RandomAnimationStart = 0x00000010,
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
    CombatBackgroundRemoveMask = 0x0fffffff
}
