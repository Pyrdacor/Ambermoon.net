using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class MusicData : IFileSpec<MusicData>, IFileSpec
{
    public static string Magic => "MUS";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    byte[]? songData = null;

    public byte[] SongData => songData!;

    public MusicData()
    {

    }

    public MusicData(byte[] songData)
    {
        this.songData = songData;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        songData = dataReader.ReadToEnd();
    }

    public void Write(IDataWriter dataWriter)
    {
        if (songData == null)
            throw new AmbermoonException(ExceptionScope.Application, "Music data was null when trying to write it.");

        dataWriter.Write(songData);
    }
}
