namespace Ambermoon.Data.Legacy
{
    public class TilesetReader : ITilesetReader
    {
        public void ReadTileset(Tileset tileset, IDataReader dataReader)
        {
            int numTiles = dataReader.ReadWord();
            tileset.Tiles = new Tileset.Tile[numTiles];

            for (int i = 0; i < numTiles; ++i)
            {
                var tileFlags = dataReader.ReadDword();

                tileset.Tiles[i] = new Tileset.Tile();
                tileset.Tiles[i].GraphicIndex = dataReader.ReadWord();
                tileset.Tiles[i].NumAnimationFrames = dataReader.ReadByte();
                tileset.Tiles[i].Unknown2 = dataReader.ReadByte(); // Unknown
                tileset.Tiles[i].Flags = tileFlags; // TODO: REMOVE later

                ParseTileFlags(tileset.Tiles[i], tileFlags);
            }

            // TODO
            /* Daniel Schulz from slothsoft has a color dictionary in his data:
                <option key="0" val="00 transparent" />
				<option key="1" val="01 white" />
				<option key="2" val="02 light gray" />
				<option key="3" val="03 medium gray" />
				<option key="4" val="04 dark gray" />
				<option key="5" val="05 really dark gray" />
				<option key="6" val="06 dark brown" />
				<option key="7" val="07 dark yellow" />
				<option key="8" val="08 medium brown" />
				<option key="9" val="09 light brown" />
				<option key="10" val="10" />
				<option key="11" val="11" />
				<option key="12" val="12 blue" />
				<option key="13" val="13 light blue" />
				<option key="14" val="14 yellow" />
				<option key="15" val="15 orange" />
               And he said that Unknown2 above is this color index.
            See: https://github.com/Faulo/slothsoft-amber/blob/master/assets/games/ambermoon/convert/lib.dictionaries.xsl */
        }

        void ParseTileFlags(Tileset.Tile tile, ulong flags)
        {
            // Bit 2: Draw in background
            // Bit 6: Draw above player (not sure as it is in combination with bit 2 often, but it seems to work if this overrides bit 2)
            // Bit 8-18: Travel type allowed flags (1 means allowed, 0 means not allowed/blocking).
            // Bit 23-25: Sit/sleep value
            //  0 -> no sitting nor sleeping
            //  1 -> sit and look up
            //  2 -> sit and look right
            //  3 -> sit and look down
            //  4 -> sit and look left
            //  5 -> sleep (always face down)
            // Bit 26: Player invisible (doors, behind towers/walls, etc)

            // Another possible explanation for bit 2/6 would be:
            // - Bit 2: Disable baseline rendering / use custom sprite ordering
            // - Bit 6: 0 = behind player, 1 = above player (only used if Bit 2 is set)

            tile.Background = (flags & 0x04) != 0;
            tile.BringToFront = (flags & 0x40) != 0;
            tile.AllowedTravelTypes = (ushort)((flags >> 8) & 0x7ff);
            var sitSleepValue = (flags >> 23) & 0x07;
            tile.SitDirection = (sitSleepValue == 0 || sitSleepValue > 4) ? (CharacterDirection?)null : (CharacterDirection)(sitSleepValue - 1);
            tile.Sleep = sitSleepValue == 5;
            tile.Invisible = (flags & 0x04000000) != 0;
        }
    }
}
