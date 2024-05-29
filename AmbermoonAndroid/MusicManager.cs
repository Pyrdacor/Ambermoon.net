using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;

namespace AmbermoonAndroid
{
    class MusicManager : ISongManager
    {
        IAudioOutput audioOutput = null;
        IAudioStream currentStream = null;
        protected static readonly Song[] Songs = EnumHelper.GetValues<Song>().Skip(1).ToArray();
        readonly ISongManager songManager = null;
        readonly object startMutex = new();

        public MusicManager(IGameData gameData)
        {
            songManager = gameData.SongManager;
        }

        public ISong GetSong(Song index)
        {
            return songManager.GetSong(index);
        }

        public void Start(IAudioOutput audioOutput, IAudioStream audioStream, int channels, int sampleRate, bool sample8Bit)
        {
            lock (startMutex)
            {
                this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

                if (currentStream != audioStream)
                {
                    Stop();
                    currentStream = audioStream;
                    audioOutput.StreamData(audioStream, channels, sampleRate, sample8Bit);
                }
                if (!audioOutput.Streaming)
                    audioOutput.Start();
            }
        }

        public void Stop()
        {
            audioOutput?.Stop();
            audioOutput?.Reset();
            currentStream = null;
        }
    }
}
