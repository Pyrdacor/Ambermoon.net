using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class IntroAssetData : IFileSpec<IntroAssetData>, IFileSpec
{
    public static string Magic => "IAD";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    IntroAssets? assets;

    public IntroAssets Assets => assets!;

    public IntroAssetData()
    {

    }

    public IntroAssetData(IntroAssets introGraphicsInfo)
    {
        this.assets = introGraphicsInfo;
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
