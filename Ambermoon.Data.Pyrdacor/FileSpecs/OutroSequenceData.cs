using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class OutroSequenceData : IFileSpec<OutroSequenceData>, IFileSpec
{
    public static string Magic => "OSQ";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Dictionary<OutroOption, IReadOnlyList<OutroAction>>? sequences;

    public IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> Sequences => sequences!.AsReadOnly();

    public OutroSequenceData()
    {

    }

    public OutroSequenceData(IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> sequences)
    {
        this.sequences = new(sequences);
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int numSequences = dataReader.ReadByte();

        sequences = new(numSequences);

        for (int i = 0; i < numSequences; i++)
        {
            var option = (OutroOption)i;
            int numActions = dataReader.ReadByte();
            var actions = new List<OutroAction>(numActions);

            for (int j = 0; j < numActions; j++)
            {
                var command = (OutroCommand)dataReader.ReadByte();

                switch (command)
                {
                    case OutroCommand.WaitForClick:
                        actions.Add(new OutroAction { Command = command });
                        break;
                    case OutroCommand.ChangePicture:
                        actions.Add(new OutroAction { Command = command, ImageOffset = dataReader.ReadWord() });
                        break;
                    case OutroCommand.PrintTextAndScroll:
                        actions.Add(new OutroAction { Command = command, LargeText = dataReader.ReadBool(), ScrollAmount = dataReader.ReadWord(), TextDisplayX = dataReader.ReadByte(), TextIndex = dataReader.ReadWord() });
                        break;
                }
            }

            sequences.Add(option, actions);
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        if (sequences == null || sequences.Count == 0)
            throw new NullReferenceException("Sequences was null or empty.");

        dataWriter.Write((byte)sequences.Count);

        for (int i = 0; i < sequences.Count; i++)
        {
            var option = (OutroOption)i;
            var actions = sequences[option];

            dataWriter.Write((byte)actions.Count);            

            foreach (var action in actions)
            {
                dataWriter.Write((byte)action.Command);

                switch (action.Command)
                {
                    case OutroCommand.ChangePicture:
                        dataWriter.Write((ushort)action.ImageOffset!.Value);
                        break;
                    case OutroCommand.PrintTextAndScroll:
                        dataWriter.Write(action.LargeText);
                        dataWriter.Write((ushort)action.ScrollAmount);
                        dataWriter.Write((byte)action.TextDisplayX);
                        dataWriter.Write((ushort)action.TextIndex!.Value);
                        break;
                }
            }
        }
    }
}
