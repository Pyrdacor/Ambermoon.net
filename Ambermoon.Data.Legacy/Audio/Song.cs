using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using SonicArranger;
using System;

namespace Ambermoon.Data.Legacy.Audio
{
    public interface ISonicArrangerSongInfo
    {
        int SongLength { get; }
        int PatternLength { get; }
        int InterruptsPerSecond { get; }
        int InitialSongSpeed { get; }
        int InitialBeatsPerMinute { get; }
    }

    public interface ISongDataProvider
    {
        byte[] GetData();
    }

    class Song : ISong, ISonicArrangerSongInfo, ISongDataProvider, IAudioStream
    {
        readonly SonicArranger.Song sonicArrangerSong;
        readonly SongPlayer songPlayer;
        readonly SonicArrangerFile sonicArrangerFile;
        readonly Enumerations.Song song;
        Stream stream = null;

        public int SongLength => sonicArrangerSong.StopPos - sonicArrangerSong.StartPos;
        public int PatternLength => sonicArrangerSong.PatternLength;
        public int InterruptsPerSecond => sonicArrangerSong.NBIrqps;
        public int InitialSongSpeed => sonicArrangerSong.SongSpeed;
        public int InitialBeatsPerMinute => sonicArrangerSong.InitialBPM;
        public TimeSpan? SongDuration { get; private set; } = null;

        public Song(Enumerations.Song song, int songIndex, SongPlayer songPlayer, DataReader reader,
            Stream.ChannelMode channelMode, bool hardwareLPF, bool pal)
        {
            this.song = song;
            this.songPlayer = songPlayer;
            reader.Position = 0;
            sonicArrangerFile = new SonicArrangerFile(reader);
            sonicArrangerSong = sonicArrangerFile.Songs[songIndex];
            stream = new Stream(sonicArrangerFile, songIndex, 44100, channelMode, hardwareLPF, pal);
        }

        Enumerations.Song ISong.Song => song;

        public bool EndOfStream => stream != null && stream.EndOfStream;

        public void Play(IAudioOutput audioOutput)
        {
            songPlayer.Start(audioOutput, this);
        }

        public void Stop()
        {
            songPlayer.Stop();
        }

        public byte[] GetData()
        {
            return stream?.ToUnsignedArray();
        }

        public byte[] Stream(TimeSpan duration)
        {
            return stream.ReadUnsigned(Util.Round(duration.TotalMilliseconds), false);
        }
    }
}
