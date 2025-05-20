using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class LabyrinthData : IFileSpec<LabyrinthData>, IFileSpec
{
    public static string Magic => "LAB";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();

    public void Read(IDataReader dataReader, uint _, GameData __)
    {
        throw new NotImplementedException();
    }

    public void Write(IDataWriter dataWriter)
    {
        throw new NotImplementedException();
    }
}
