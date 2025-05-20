using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class TilesetData : IFileSpec<TilesetData>, IFileSpec
{
    public static string Magic => "TIL";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Tileset? tileset = null;

    public Tileset Tileset => tileset!;

    public TilesetData()
    {

    }

    public TilesetData(Tileset tileset)
    {
        this.tileset = tileset;
    }

    public void Read(IDataReader dataReader, uint _, GameData __)
    {
        tileset = new Tileset();
        new TilesetReader().ReadTileset(tileset, dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (tileset == null)
            throw new AmbermoonException(ExceptionScope.Application, "Tileset data was null when trying to write it.");

        TilesetWriter.WriteTileset(tileset, dataWriter);
    }
}
