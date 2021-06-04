using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using SonicArranger;

namespace Ambermoon.Data.Legacy.Audio
{
    class Song : ISong
    {
        readonly SongPlayer songPlayer;
        readonly SonicArrangerFile sonicArrangerFile;
        readonly Stream stream;

        public Song(SongPlayer songPlayer, DataReader reader, bool stereo, bool hardwareLPF, bool pal)
        {
            this.songPlayer = songPlayer;
            sonicArrangerFile = new SonicArrangerFile(reader);
            stream = new Stream(sonicArrangerFile, 0, 44100, stereo, hardwareLPF, pal);
        }

        public void Play()
        {
            songPlayer.Start(stream);
        }

        public void Stop()
        {
            songPlayer.Stop();
        }
    }
}
