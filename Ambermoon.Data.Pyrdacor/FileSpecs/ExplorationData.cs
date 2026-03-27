using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class ExplorationData : IFileSpec<ExplorationData>, IFileSpec
{
    private static readonly AutomapReader automapReader = new();
    private static readonly AutomapWriter automapWriter = new();

    public static string Magic => "EXP";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Automap? automap = null;

    public Automap Automap => automap!;

    public ExplorationData()
    {

    }

    public ExplorationData(Automap automap)
    {
        this.automap = automap;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        automap = Automap.Load(automapReader, dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (automap == null)
            throw new AmbermoonException(ExceptionScope.Application, "Automap data was null when trying to write it.");

        automapWriter.WriteAutomap(automap, dataWriter);
    }
}
