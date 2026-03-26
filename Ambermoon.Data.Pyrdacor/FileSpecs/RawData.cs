using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class RawData : IFileSpec<RawData>, IFileSpec
{
    public static string Magic => "RAW";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    byte[]? data = null;

    public byte[] Data => data!;

    public RawData()
    {

    }

    public RawData(byte[] data)
    {
        this.data = data;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int size = (int)(dataReader.ReadDword() & int.MaxValue);
        data = dataReader.ReadBytes(size);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (data == null)
            throw new AmbermoonException(ExceptionScope.Application, "Raw data was null when trying to write it.");

        uint size = (uint)data.Length;
        dataWriter.Write(size);
        dataWriter.Write(data);
    }
}
