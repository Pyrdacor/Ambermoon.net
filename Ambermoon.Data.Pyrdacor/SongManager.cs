using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon.Data.Pyrdacor;

internal class SongManager : ISongManager
{
    readonly Legacy.Audio.SongManager songManager;

    public SongManager(Dictionary<uint, byte[]> songs)
    {
        var songDataReaders = new Dictionary<Song, DataReader>(songs.Count + 1);

        foreach (var song in songs)
        {
            var songKey = (Song)(byte)song.Key;
            var dataReader = new DataReader(song.Value);

            songDataReaders.Add(songKey, dataReader);

            if (songKey == Song.Intro)
                songDataReaders.Add(Song.Menu, dataReader);
        }

        songManager = new Legacy.Audio.SongManager(songDataReaders);
    }

    public ISong GetSong(Song index) => songManager.GetSong(index);
}
