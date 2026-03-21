using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class FantasyIntroAssetData : IFileSpec<FantasyIntroAssetData>, IFileSpec
{
    public static string Magic => "FIA";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    FantasyIntroAssets? assets;

    public FantasyIntroAssets Assets => assets!;

    public FantasyIntroAssetData()
    {

    }

    public FantasyIntroAssetData(FantasyIntroAssets assets)
    {
        this.assets = assets;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        assets = new(dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        Assets.Write(dataWriter);
    }
}
