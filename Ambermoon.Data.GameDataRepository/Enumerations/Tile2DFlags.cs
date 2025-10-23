using Ambermoon.Data.GameDataRepository.Util;

namespace Ambermoon.Data.GameDataRepository.Enumerations;

[Flags]
public enum Tile2DFlags : uint
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
    /// Normally the lower half of the player is drawn
    /// above underlying tiles but below overlaying tiles.
    /// But the upper half is drawn above everything.
    ///
    /// If the custom render order is activated,
    /// <see cref="CustomRenderOrderMode"/> will determine
    /// the player's render order.
    /// </summary>
    CustomRenderOrder = 0x00000004,
    /// <summary>
    /// Normally animations are started at map entering
    /// and will just loop continuously. If this is set,
    /// the animation is started at a random frame and
    /// after a full cycle/wave it will stop and wait
    /// for the next random start.
    /// </summary>
    RandomAnimationStart = 0x00000010,
    /// <summary>
    /// This is only considered for overlays. If set, they
    /// will use the flags of the underlying tile. This is
    /// often used to inherit the blocking modes of the tile.
    /// </summary>
    UseBackgroundTileFlags = 0x00000020,
    /// <summary>
    /// This is only used if <see cref="CustomRenderOrder"/>
    /// is set. It changes the order of rendering the player.
    ///
    /// If this is unset, the player will be drawn above
    /// the tile, even the lower half and even if this is
    /// a foreground/overlay tile.
    /// Usage example: Carpet on top of the floor.
    ///
    /// If this is set, the player's upper half will be
    /// drawn below the tile if this is an overlay.
    /// Usage example: Large tree top (2+ tiles high).
    /// </summary>
    CustomRenderOrderMode = 0x00000040,
    /// <summary>
    /// Blocks all movement. Monsters and the player can't
    /// enter the tile, no matter the transportation they use.
    ///
    /// This is a general switch and will override all other
    /// movement allow bits below if it is set.
    /// </summary>
    BlockAllMovement = 0x00000080,
    /// <summary>
    /// Allows the player to enter the tile by walking.
    /// </summary>
    AllowMovementWalk = 0x00000100,
    /// <summary>
    /// Allows the player to enter the tile by horse.
    /// </summary>
    AllowMovementHorse = 0x00000200,
    /// <summary>
    /// Allows the player to enter the tile by raft.
    /// </summary>
    AllowMovementRaft = 0x00000400,
    /// <summary>
    /// Allows the player to enter the tile by ship.
    /// </summary>
    AllowMovementShip = 0x00000800,
    /// <summary>
    /// Allows the player to enter the tile by magic disc.
    /// </summary>
    AllowMovementMagicDisc = 0x00001000,
    /// <summary>
    /// Allows the player to enter the tile by eagle.
    /// </summary>
    AllowMovementEagle = 0x00002000,
    /// <summary>
    /// Allows the player to enter the tile by flying.
    /// </summary>
    AllowMovementFly = 0x00004000,
    /// <summary>
    /// Allows the player to enter the tile by swimming.
    ///
    /// Note: This basically marks tiles as water tiles.
    /// In contrast to the other movement types, the player
    /// will automatically switch between walking and swimming
    /// when entering/leaving the tile.
    ///
    /// This is only possible on world maps.
    /// </summary>
    AllowMovementSwim = 0x00008000,
    /// <summary>
    /// Allows the player to enter the tile by broom.
    /// </summary>
    AllowMovementBroom = 0x00010000,
    /// <summary>
    /// Allows the player to enter the tile by sand lizard.
    /// </summary>
    AllowMovementSandLizard = 0x00020000,
    /// <summary>
    /// Allows the player to enter the tile by sand ship.
    /// </summary>
    AllowMovementSandShip = 0x00040000,
    /// <summary>
    /// Allows the player to enter the tile by wasp.
    ///
    /// Advanced only.
    /// </summary>
    [AdvancedOnly]
    AllowMovementWasp = 0x00080000,
    /// <summary>
    /// Mask for the sit/sleep flags. The 3 bits
    /// have the following meaning:
    ///
    /// 0: no sitting nor sleeping
    /// 1: sit and look up
    /// 2: sit and look right
    /// 3: sit and look down
    /// 4: sit and look left
    /// 5: sleep (always face down)
    /// 6-7: unused
    /// </summary>
    SitSleepMask = 0x03800000,
    /// <summary>
    /// If set, the player becomes invisible
    /// when entering the tile. This is often
    /// used for doors.
    /// </summary>
    HidePlayer = 0x04000000,
    /// <summary>
    /// If set, the player will be poisoned when
    /// entering the tile. But only at the first
    /// frame of the animation. Often used in
    /// combination with <see cref="RandomAnimationStart"/>.
    /// </summary>
    AutoPoison = 0x08000000,
    /// <summary>
    /// Mask for the combat background index.
    /// </summary>
    CombatBackgroundMask = 0xf0000000
}
