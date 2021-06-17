using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using SonicArranger;
using System;
using System.Threading.Tasks;

namespace Ambermoon.Data.Legacy.Audio
{
    class Song : ISong
    {
        readonly SongPlayer songPlayer;
        readonly SonicArrangerFile sonicArrangerFile;
        readonly Enumerations.Song song;
        byte[] buffer;
        readonly Task loadTask = null;

        public Song(Enumerations.Song song, int songIndex, SongPlayer songPlayer, DataReader reader,
            Stream.ChannelMode channelMode, bool hardwareLPF, bool pal, bool waitForLoading = false, Action loadFinishedHandler = null)
        {
            this.song = song;
            this.songPlayer = songPlayer;
            reader.Position = 0;
            sonicArrangerFile = new SonicArrangerFile(reader);
            void Load()
            {
                buffer = new Stream(sonicArrangerFile, songIndex, 44100, channelMode, hardwareLPF, pal).ToUnsignedArray();
                loadFinishedHandler?.Invoke();
            }
            if (waitForLoading)
                Load();
            else
                loadTask = Task.Run(Load);
        }

        Enumerations.Song ISong.Song => song;

        public void Play(IAudioOutput audioOutput)
        {
            if (buffer == null)
                loadTask.GetAwaiter().OnCompleted(Start);
            else
                Start();

            void Start() => songPlayer.Start(audioOutput, buffer);
        }

        public void Stop()
        {
            songPlayer.Stop();
        }
    }
}
