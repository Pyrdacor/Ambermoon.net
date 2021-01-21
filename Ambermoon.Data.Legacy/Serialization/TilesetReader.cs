using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
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
                tileset.Tiles[i].Unknown = dataReader.ReadByte();
                tileset.Tiles[i].Flags = (Tileset.TileFlags)tileFlags;
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
    }
}
