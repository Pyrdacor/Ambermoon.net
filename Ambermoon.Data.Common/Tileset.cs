using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Tileset
    {
        public enum TileFlags
        {
            None = 0,
            Background = 0x04,
            Transparency = 0x08, // TODO: Is this also true for 2D?
            UseBackgroundTileFlags = 0x20,
            BringToFront = 0x40,
            BlockAllMovement = 0x80,
            PlayerInvisible = 0x04000000
        }

        public class Tile
        {
            TileFlags flags = TileFlags.None;

            public uint GraphicIndex { get; set; }
            public int NumAnimationFrames { get; set; }
            public CharacterDirection? SitDirection { get; set; }
            public bool Sleep { get; set; }
            public bool CharacterInvisible => flags.HasFlag(TileFlags.PlayerInvisible); // player is invisible while standing on that tile
            public bool Background => flags.HasFlag(TileFlags.Background); // used by foreground tiles which should appear in the back (e.g. carpets)
            public bool BringToFront => flags.HasFlag(TileFlags.BringToFront); // overrides Background and will appear above the player (e.g. tree tops)
            public bool UseBackgroundTileFlags => flags.HasFlag(TileFlags.UseBackgroundTileFlags);
            public TileFlags Flags
            {
                get => flags;
                set
                {
                    flags = value;
                    ProcessFlags();
                }
            }
            public ushort AllowedTravelTypes { get; set; }
            public uint CombatBackgroundIndex { get; set; }
            public byte Unknown { get; set; }

            public bool AllowMovement(TravelType travelType) => !Flags.HasFlag(TileFlags.BlockAllMovement) && (AllowedTravelTypes & (1 << (int)travelType)) != 0;

            public static void ProcessFlags(Tile tile, TileFlags flags)
            {
                // Note: I guess tiles, 3D objects, 3D walls and map characters all use the same 32 bit flags.
                // Some are only useful in 2D, some only in 3D and some only on map or character.

                // Walls:
                // BlockSight = 0x02, // Not sure but beside walls this is also used by non-bocking doors or exits
                // Transparency = 0x08,
                // BlockMovement = 0x80,

                // Objects:
                // FloorObject = 0x08, // like holes in the ground
                // BlockMovement = 0x80,

                // 2D Map tiles:
                // Bit 1: Draw partial in background? Bottom of a wall in the back?
                // Bit 2: Draw in background
                // Bit 6: Draw above player (not sure as it is in combination with bit 2 often, but it seems to work if this overrides bit 2)
                // Bit 7: Block all movement if set?
                // Bit 8-18: Travel type allowed flags (1 means allowed, 0 means not allowed/blocking).
                // Bit 23-25: Sit/sleep value
                //  0 -> no sitting nor sleeping
                //  1 -> sit and look up
                //  2 -> sit and look right
                //  3 -> sit and look down
                //  4 -> sit and look left
                //  5 -> sleep (always face down)
                // Bit 26: Player invisible (doors, behind towers/walls, etc)
                // Bit 28-31: Combat background index when battle event is triggered on that tile

                // Another possible explanation for bit 2/6 would be:
                // - Bit 2: Disable baseline rendering / use custom sprite ordering
                // - Bit 6: 0 = behind player, 1 = above player (only used if Bit 2 is set)

                uint flagValue = (uint)flags;

                tile.AllowedTravelTypes = (ushort)((flagValue >> 8) & 0x7ff);
                var sitSleepValue = (flagValue >> 23) & 0x07;
                tile.SitDirection = (sitSleepValue == 0 || sitSleepValue > 4) ? (CharacterDirection?)null : (CharacterDirection)(sitSleepValue - 1);
                tile.Sleep = sitSleepValue == 5;
                tile.CombatBackgroundIndex = flagValue >> 28;
            }

            void ProcessFlags()
            {
                ProcessFlags(this, Flags);
            }
        }

        public uint Index { get; set; }
        public Tile[] Tiles { get; set; }

        private Tileset()
        {

        }

        public static Tileset Load(ITilesetReader tilesetReader, IDataReader dataReader)
        {
            var tileset = new Tileset();

            tilesetReader.ReadTileset(tileset, dataReader);

            return tileset;
        }

        public bool AllowMovement(uint backgroundTile, uint foregroundTile, TravelType travelType)
        {
            if (foregroundTile == 0)
                return Tiles[backgroundTile - 1].AllowMovement(travelType);

            if (backgroundTile == 0)
                return Tiles[foregroundTile - 1].AllowMovement(travelType);

            var foreground = Tiles[foregroundTile - 1];

            return foreground.UseBackgroundTileFlags
                ? Tiles[backgroundTile - 1].AllowMovement(travelType)
                : foreground.AllowMovement(travelType);
        }
    }
}
