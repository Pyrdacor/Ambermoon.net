using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class IntroAssets
{
    readonly Dictionary<IntroGraphic, Size> graphicSizes = [];
    readonly List<IntroTwinlakeImagePart> twinlakeImageParts = [];
    readonly List<IIntroTextCommand> textCommands = [];
    readonly List<string> textCommandTexts = [];

    public IReadOnlyDictionary<IntroGraphic, Size> GraphicSizes => graphicSizes.AsReadOnly();
    public IReadOnlyList<IIntroTwinlakeImagePart> TwinlakeImageParts => twinlakeImageParts.AsReadOnly();
    public IReadOnlyList<IIntroTextCommand> TextCommands => textCommands.AsReadOnly();
    public IReadOnlyList<string> TextCommandTexts => textCommandTexts.AsReadOnly();

    public IntroAssets()
    {

    }

    public IntroAssets
    (
        Dictionary<IntroGraphic, Size> graphicSizes,
        List<IntroTwinlakeImagePart> twinlakeImageParts,
        List<TextCommand> textCommands,
        List<string> textCommandTexts
    )
    {
        this.graphicSizes = graphicSizes;
        this.twinlakeImageParts = twinlakeImageParts;
        this.textCommands = textCommands.Cast<IIntroTextCommand>().ToList();
        this.textCommandTexts = textCommandTexts;
    }

    public IntroAssets(IDataReader dataReader)
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

        textCommands.Clear();
        textCommandTexts.Clear();

        while (TextCommand.TryParse(dataReader, textCommandTexts, out var textCommand))
            textCommands.Add(textCommand!);
    }

    private void WriteTextCommands(IDataWriter dataWriter)
    {
        foreach (var textCommand in TextCommands)
        {
            dataWriter.WriteEnum8(textCommand.Type);

            switch (textCommand.Type)
            {
                case IntroTextCommandType.Add:
                    dataWriter.Write((byte)textCommand.Args[0]); // X
                    dataWriter.Write((byte)textCommand.Args[1]); // Y
                    string text = TextCommandTexts[textCommand.Args[2]];
                    dataWriter.WriteNullTerminated(text);
                    break;
                case IntroTextCommandType.Wait:
                    dataWriter.Write((byte)textCommand.Args[0]); // Ticks
                    break;
                case IntroTextCommandType.SetTextColor:
                    dataWriter.Write((ushort)textCommand.Args[0]); // Color
                    break;
                default:
                    // No args
                    break;
            }
        }

        dataWriter.Write((byte)255); // end marker
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

        WriteTextCommands(dataWriter);
    }
}
