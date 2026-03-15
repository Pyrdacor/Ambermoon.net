using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class Palette : IFileSpec<Palette>, IFileSpec
{
    public static string Magic => "PAL";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Graphic? graphic = null;

    public Graphic Graphic => graphic!;
    public byte DefaultTextPaletteIndex { get; set; }
    public byte PrimaryUIPaletteIndex { get; set; }
    public byte SecondaryUIPaletteIndex { get; set; }
    public byte AutomapPaletteIndex { get; set; }
    public byte FirstIntroPaletteIndex { get; set; }
    public byte FirstOutroPaletteIndex { get; set; }
    public byte FirstFantasyIntroPaletteIndex { get; set; }

    public Palette()
    {

    }

    public Palette(Graphic graphic)
    {
        this.graphic = graphic;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int paletteCount = dataReader.ReadByte();

        PrimaryUIPaletteIndex = dataReader.ReadByte();
        AutomapPaletteIndex = dataReader.ReadByte();
        SecondaryUIPaletteIndex = dataReader.ReadByte();
        FirstIntroPaletteIndex = dataReader.ReadByte();
        FirstOutroPaletteIndex = dataReader.ReadByte();
        FirstFantasyIntroPaletteIndex = dataReader.ReadByte();

        graphic = new Graphic();

        new GraphicReader().ReadGraphic(graphic, dataReader, new GraphicInfo
        {
            Width = 32,
            Height = paletteCount,
            GraphicFormat = GraphicFormat.RGBA32,
            Alpha = false
        });
    }

    public void Write(IDataWriter dataWriter)
    {
        if (graphic == null)
            throw new AmbermoonException(ExceptionScope.Application, "Palette data was null when trying to write it.");

        dataWriter.Write((byte)graphic.Height);

        dataWriter.Write((byte)PrimaryUIPaletteIndex);
        dataWriter.Write((byte)AutomapPaletteIndex);
        dataWriter.Write((byte)SecondaryUIPaletteIndex);
        dataWriter.Write((byte)FirstIntroPaletteIndex);
        dataWriter.Write((byte)FirstOutroPaletteIndex);
        dataWriter.Write((byte)FirstFantasyIntroPaletteIndex);

        dataWriter.Write(graphic.Data);
    }
}
