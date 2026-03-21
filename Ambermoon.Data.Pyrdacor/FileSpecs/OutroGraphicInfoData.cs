using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

using OutroGraphicInfo = Objects.OutroGraphicInfo;

internal class OutroGraphicInfoData : IFileSpec<OutroGraphicInfoData>, IFileSpec
{
    public static string Magic => "OGI";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<NullCompression>();
    OutroGraphicInfo? outroGraphicInfo;

    public OutroGraphicInfo OutroGraphicInfo => outroGraphicInfo!.Value;

    public OutroGraphicInfoData()
    {

    }

    public OutroGraphicInfoData(Data.OutroGraphicInfo outroGraphicInfo, uint imageDataOffset)
    {
        this.outroGraphicInfo = new()
        {
            ImageDataOffset = imageDataOffset,
            Width = outroGraphicInfo.Width,
            Height = outroGraphicInfo.Height,
            PaletteIndex = outroGraphicInfo.PaletteIndex
        };
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        uint imageDataOffset = dataReader.ReadDword();
        int width = dataReader.ReadWord();
        int height = dataReader.ReadWord();
        byte paletteIndex = dataReader.ReadByte();

        outroGraphicInfo = new()
        {
            ImageDataOffset = imageDataOffset,
            Width = width,
            Height = height,
            PaletteIndex = paletteIndex
        };
    }

    public void Write(IDataWriter dataWriter)
    {
        var outroGraphicInfo = OutroGraphicInfo;

        dataWriter.Write((uint)outroGraphicInfo.ImageDataOffset);
        dataWriter.Write((ushort)outroGraphicInfo.Width);
        dataWriter.Write((ushort)outroGraphicInfo.Height);
        dataWriter.Write((byte)outroGraphicInfo.PaletteIndex);
    }
}
