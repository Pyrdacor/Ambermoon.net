using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class OutroGraphicsInfoData : IFileSpec<OutroGraphicsInfoData>, IFileSpec
{
    public static string Magic => "OGI";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    OutroGraphicInfo? outroGraphicInfo;

    public OutroGraphicInfo OutroGraphicInfo => outroGraphicInfo!.Value;

    public OutroGraphicsInfoData()
    {

    }

    public OutroGraphicsInfoData(OutroGraphicInfo outroGraphicInfo)
    {
        this.outroGraphicInfo = outroGraphicInfo;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        uint index = dataReader.ReadByte();
        int width = dataReader.ReadWord();
        int height = dataReader.ReadWord();
        byte paletteIndex = dataReader.ReadByte();

        outroGraphicInfo = new()
        {
            GraphicIndex = index,
            Width = width,
            Height = height,
            PaletteIndex = paletteIndex
        };
    }

    public void Write(IDataWriter dataWriter)
    {
        var outroGraphicInfo = OutroGraphicInfo;

        dataWriter.Write((byte)outroGraphicInfo.GraphicIndex);
        dataWriter.Write((ushort)outroGraphicInfo.Width);
        dataWriter.Write((ushort)outroGraphicInfo.Height);
        dataWriter.Write((byte)outroGraphicInfo.PaletteIndex);
    }
}
