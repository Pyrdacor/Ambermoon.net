using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using SonicArranger;
using System.Threading;
using System.Threading.Tasks;

namespace Ambermoon.Data.Legacy.Audio
{
    class Song : ISong
    {
        readonly SongPlayer songPlayer;
        readonly SonicArrangerFile sonicArrangerFile;
        readonly Enumerations.Song song;
        byte[] buffer;
        readonly Task loadTask;

        public Song(Enumerations.Song song, SongPlayer songPlayer, DataReader reader, bool stereo, bool hardwareLPF, bool pal)
        {
            this.song = song;
            this.songPlayer = songPlayer;
            sonicArrangerFile = new SonicArrangerFile(reader);
            loadTask = Task.Run(() => buffer = new Stream(sonicArrangerFile, 0, 44100, stereo, hardwareLPF, pal).ToUnsignedArray());
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
