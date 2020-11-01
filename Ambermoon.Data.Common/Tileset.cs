using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Tileset
    {
        public class Tile
        {
            public uint GraphicIndex { get; set; }
            public int NumAnimationFrames { get; set; }
            public CharacterDirection? SitDirection { get; set; }
            public bool Sleep { get; set; }
            public bool Invisible { get; set; } // player is invisible while standing on that tile
            public bool Background { get; set; } // used by foreground tiles which should appear in the back (e.g. carpets)
            public bool BringToFront { get; set; } // overrides Background and will appear above the player (e.g. tree tops)
            public byte Unknown2 { get; set; } // TODO: What is this? Remove if unused later.
            public ulong Flags { get; set; } // TODO: REMOVE later
            public ushort AllowedTravelTypes { get; set; }

            public bool AllowMovement(TravelType travelType) => (AllowedTravelTypes & (1 << (int)travelType)) != 0;
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
    }
}
