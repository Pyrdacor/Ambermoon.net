using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class IntroGraphicsInfo : IFileSpec<IntroGraphicsInfo>, IFileSpec
{
    public static string Magic => "IGI";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    readonly Dictionary<IntroGraphic, Size> graphicSizes = [];
    readonly List<IntroTwinlakeImagePart> twinlakeImageParts = [];

    public IReadOnlyDictionary<IntroGraphic, Size> GraphicSizes => graphicSizes.AsReadOnly();
    public IReadOnlyList<IIntroTwinlakeImagePart> TwinlakeImageParts => twinlakeImageParts.AsReadOnly();

    public IntroGraphicsInfo()
    {

    }

    public IntroGraphicsInfo(Dictionary<IntroGraphic, Size> graphicSizes, List<IntroTwinlakeImagePart> twinlakeImageParts)
    {
        this.graphicSizes = graphicSizes;
        this.twinlakeImageParts = twinlakeImageParts;
    }

    public IntroGraphicsInfo(IDataReader dataReader)
    {
        int graphicCount = dataReader.ReadByte();

        graphicSizes.Clear();

        for (int i = 0; i < graphicCount; i++)
        {
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();

            graphicSizes.Add((IntroGraphic)i, new()
            {
                Width = width,
                Height = height,
            });
        }

        int twinlakeGraphicCount = dataReader.ReadByte();

        twinlakeImageParts.Clear();

        for (int i = 0; i < twinlakeGraphicCount; i++)
        {
            int x = dataReader.ReadWord();
            int y = dataReader.ReadWord();
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();
            var colorIndices = dataReader.ReadBytes(width * height);

            twinlakeImageParts.Add(new()
            {
                Position = new(x, y),
                Graphic = new()
                {
                    Width = width,
                    Height = height,
                    Data = colorIndices,
                    IndexedGraphic = true,
                }
            });
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((byte)graphicSizes.Count);

        foreach (var graphicSize in graphicSizes.OrderBy(g => g.Key).Select(g => g.Value))
        {
            dataWriter.Write((ushort)graphicSize.Width);
            dataWriter.Write((ushort)graphicSize.Height);
        }

        dataWriter.Write((byte)twinlakeImageParts.Count);

        foreach (var twinlakeImagePart in twinlakeImageParts)
        {
            if (twinlakeImagePart.Graphic.Data == null || !twinlakeImagePart.Graphic.IndexedGraphic || twinlakeImagePart.Graphic.Data.Length != twinlakeImagePart.Graphic.Width * twinlakeImagePart.Graphic.Height)
                throw new InvalidDataException("Invalid twinlage image part graphic data. Must be non-null and indexed.");

            dataWriter.Write((ushort)twinlakeImagePart.Position.X);
            dataWriter.Write((ushort)twinlakeImagePart.Position.Y);
            dataWriter.Write((ushort)twinlakeImagePart.Graphic.Width);
            dataWriter.Write((ushort)twinlakeImagePart.Graphic.Height);
            dataWriter.Write(twinlakeImagePart.Graphic.Data);
        }
    }
}
