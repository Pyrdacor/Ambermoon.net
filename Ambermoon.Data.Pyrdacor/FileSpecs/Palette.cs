using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class Palette : IFileSpec<Palette>, IFileSpec
{
    public static string Magic => "PAL";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Graphic? graphic = null;

    public const ushort GamePalettesIndex = 1;
    public const ushort OutroPalettesIndex = 2;
    public const ushort IntroPalettesIndex = 3;
    public const ushort FantasyIntroPalettesIndex = 4;

    public Graphic Graphic => graphic!;
    public byte DefaultTextPaletteIndex { get; set; } = 0xff;
    public byte PrimaryUIPaletteIndex { get; set; } = 0xff;
    public byte SecondaryUIPaletteIndex { get; set; } = 0xff;
    public byte AutomapPaletteIndex { get; set; } = 0xff;
    public byte FirstIntroPaletteIndex { get; set; } = 0xff;
    public byte FirstOutroPaletteIndex { get; set; } = 0xff;
    public byte FirstFantasyIntroPaletteIndex { get; set; } = 0xff;

    public Palette()
    {

    }

    public Palette(Graphic graphic)
    {
        this.graphic = graphic;
    }

    public IReadOnlyList<Graphic> Slice()
    {
        var graphic = Graphic;
        var palettes = new List<Graphic>(graphic.Height);
        var graphicData = new ReadOnlySpan<byte>(graphic.Data);
        int sourceIndex = 0;
        const int DataSize = 32 * 4;

        for (int y = 0; y < graphic.Height; y++)
        {
            var paletteData = graphicData.Slice(sourceIndex, DataSize);
            sourceIndex += DataSize;
            
            palettes.Add(new Graphic
            {
                Width = 32,
                Height = 1,
                Data = paletteData.ToArray()
            });
        }

        return palettes.AsReadOnly();
    }

    public void Read(IDataReader dataReader, uint index, GameData __, byte ___)
    {
        int paletteCount = dataReader.ReadByte();

        if (index == GamePalettesIndex)
        {
            DefaultTextPaletteIndex = dataReader.ReadByte();
            PrimaryUIPaletteIndex = dataReader.ReadByte();
            AutomapPaletteIndex = dataReader.ReadByte();
            SecondaryUIPaletteIndex = dataReader.ReadByte();
            FirstIntroPaletteIndex = dataReader.ReadByte();
            FirstOutroPaletteIndex = dataReader.ReadByte();
            FirstFantasyIntroPaletteIndex = dataReader.ReadByte();
        }

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

        if (DefaultTextPaletteIndex != 0xff)
        {
            dataWriter.Write((byte)DefaultTextPaletteIndex);
            dataWriter.Write((byte)PrimaryUIPaletteIndex);
            dataWriter.Write((byte)AutomapPaletteIndex);
            dataWriter.Write((byte)SecondaryUIPaletteIndex);
            dataWriter.Write((byte)FirstIntroPaletteIndex);
            dataWriter.Write((byte)FirstOutroPaletteIndex);
            dataWriter.Write((byte)FirstFantasyIntroPaletteIndex);
        }

        dataWriter.Write(graphic.Data);
    }
}
