using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    internal class MusicData : IFileSpec
    {
        public string Magic => "MUS";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        Song? song = null;
        byte[]? songData = null;

        public Song Song => song!;

        public MusicData()
        {

        }

        public MusicData(Song song, byte[] songData)
        {
            this.song = song;
            this.songData = songData;
        }

        public void Read(IDataReader dataReader, uint index, GameData gameData)
        {
            var initialPosition = dataReader.Position;
            song = (gameData.SongManager as SongManager)!.LoadSong(dataReader, (int)index, true, true) as Song;
            var position = dataReader.Position;
            dataReader.Position = initialPosition;
            songData = dataReader.ReadBytes(position - initialPosition);
            dataReader.Position = position;
        }

        public void Write(IDataWriter dataWriter)
        {
            if (songData == null)
                throw new AmbermoonException(ExceptionScope.Application, "Music data was null when trying to write it.");

            dataWriter.Write(songData);
        }
    }
}
