using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class FantasyIntroAssets
{
    readonly Dictionary<FantasyIntroGraphic, Size> graphicSizes = [];
    readonly List<FantasyIntroAction> actions = [];

    public IReadOnlyDictionary<FantasyIntroGraphic, Size> GraphicSizes => graphicSizes.AsReadOnly();
    public IReadOnlyList<FantasyIntroAction> Actions => actions.AsReadOnly();


    public FantasyIntroAssets()
    {

    }

    public FantasyIntroAssets
    (
        Dictionary<FantasyIntroGraphic, Size> graphicSizes,
        List<FantasyIntroAction> actions
    )
    {
        this.graphicSizes = graphicSizes;
        this.actions = actions;
    }

    public FantasyIntroAssets(IDataReader dataReader)
    {
        int graphicCount = dataReader.ReadByte();

        graphicSizes.Clear();

        for (int i = 0; i < graphicCount; i++)
        {
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();

            graphicSizes.Add((FantasyIntroGraphic)i, new()
            {
                Width = width,
                Height = height,
            });
        }

        int actionCount = dataReader.ReadWord();

        actions.Clear();

        for (int i = 0; i < actionCount; i++)
        {
            uint frames = dataReader.ReadWord();
            var command = (FantasyIntroCommand)dataReader.ReadByte();
            int numParams = dataReader.ReadByte();
            var parameters = new int[numParams];

            for (int p = 0; p < numParams; p++)
                parameters[p] = dataReader.ReadShort();

            actions.Add(new(frames, command, parameters));
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

        dataWriter.Write((byte)actions.Count);

        foreach (var action in actions)
        {
            dataWriter.Write((ushort)action.Frames);
            dataWriter.Write((byte)action.Command);
            dataWriter.Write((byte)action.Parameters.Length);

            foreach (var p in action.Parameters)
                dataWriter.WriteShort((short)p);
        }
    }
}
