﻿using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using SonicArranger;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio;

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
    byte[] GetData(int sampleRate);
}

internal class Song : ISong, ISonicArrangerSongInfo, ISongDataProvider, IAudioStream
{
    readonly SonicArranger.Song sonicArrangerSong;
    readonly SongPlayer songPlayer;
    readonly SonicArrangerFile sonicArrangerFile;
    readonly Enumerations.Song song;
    Stream stream = null;
    readonly Func<int, Stream> streamLoader;
    readonly Func<int, int> bytesPerSecondProvider;
    int bytesPerSecond = 0;
    TimeSpan? songDuration = null;
    readonly bool loop = false;

    public int SongLength => sonicArrangerSong.StopPos - sonicArrangerSong.StartPos;
    public int PatternLength => sonicArrangerSong.PatternLength;
    public int InterruptsPerSecond => sonicArrangerSong.NBIrqps;
    public int InitialSongSpeed => sonicArrangerSong.SongSpeed;
    public int InitialBeatsPerMinute => sonicArrangerSong.InitialBPM;
    public TimeSpan? SongDuration
    {
        get
        {
            if (songDuration == null && stream != null && stream.EndOfStream && bytesPerSecond != 0)
                songDuration = TimeSpan.FromMilliseconds(1000.0 * stream.ToUnsignedArray().Length / bytesPerSecond);

            return songDuration;
        }
    }

    public Song(Enumerations.Song song, int songIndex, SongPlayer songPlayer, DataReader reader,
        Stream.ChannelMode channelMode, bool hardwareLPF, bool pal)
    {
        this.song = song;
        this.songPlayer = songPlayer;
        loop = song == Enumerations.Song.Intro; // this loops to provide the start of the main menu song
        reader.Position = 0;
        sonicArrangerFile = new SonicArrangerFile(reader);
        sonicArrangerSong = sonicArrangerFile.Songs[songIndex];

        streamLoader = (sampleRate) => new Stream(sonicArrangerFile, songIndex, (uint)sampleRate, channelMode, hardwareLPF, pal);
        bytesPerSecondProvider = (sampleRate) => sampleRate * (int)channelMode;
    }

    Enumerations.Song ISong.Song => song;

    public bool EndOfStream => stream != null && stream.EndOfStream;

    public void Play(IAudioOutput audioOutput)
    {
        stream ??= streamLoader(audioOutput.SampleRate);
        bytesPerSecond = bytesPerSecondProvider(audioOutput.SampleRate);
        songPlayer.Start(audioOutput, this);
    }

    public void Stop()
    {
        songPlayer.Stop();
    }

    public byte[] GetData(int sampleRate)
    {
        return streamLoader(sampleRate).ToUnsignedArray();
    }

    public byte[] Stream(TimeSpan duration)
    {
        double remainingDuration = duration.TotalMilliseconds;
        var buffer = new List<byte>();

        do
        {
            var readDuration = Math.Min(remainingDuration, 1000.0);
            buffer.AddRange(stream.ReadUnsigned(Util.Round(readDuration), loop));
            remainingDuration -= readDuration;
        } while (remainingDuration > 0 && !stream.EndOfStream);

        return [.. buffer];
    }

    public void Reset()
    {
        stream?.Reset();
    }
}
