using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class IntroGraphicsInfoData : IFileSpec<IntroGraphicsInfoData>, IFileSpec
{
    public static string Magic => "IGI";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    IntroGraphicsInfo? graphicsInfo;

    public IntroGraphicsInfo GraphicsInfo => graphicsInfo!;

    public IntroGraphicsInfoData()
    {

    }

    public IntroGraphicsInfoData(IntroGraphicsInfo introGraphicsInfo)
    {
        this.graphicsInfo = introGraphicsInfo;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        graphicsInfo = new(dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        GraphicsInfo.Write(dataWriter);
    }
}
