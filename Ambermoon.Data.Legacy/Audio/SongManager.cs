using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<int, ISong> songs = new Dictionary<int, ISong>();

        public SongManager(IFileContainer fileContainer)
        {
            foreach (var file in fileContainer.Files)
            {
                songs.Add(file.Key, LoadSong(file.Value as DataReader));
            }
        }

        public ISong GetSong(int index) => songs.TryGetValue(index, out var song) ? song : null;

        static ISong LoadSong(DataReader dataReader)
        {
            var sonicArrangerFile = new SonicArranger.SonicArrangerFile(dataReader);

            // TODO: REMOVE
            for (int i = 0; i < sonicArrangerFile.Samples.Length; ++i)
            {
                WriteWave($@"D:\Programmierung\C#\Projects\Ambermoon\ambermoon.net\Test{i + 1:000}.wav",
                    sonicArrangerFile.Samples[i].Data, 3000u / (uint)sonicArrangerFile.Songs[0].SongSpeed);
            }

            // TODO

            return null;
        }

        // TODO: REMOVE
        static void WriteWave(string filename, byte[] data, uint bpm)
        {
            uint dataSamplesPerSecond = bpm * 60u;
            uint numsamples = (uint)data.Length;
            ushort numchannels = 1;
            ushort samplelength = 1; // in bytes
            uint samplerate = dataSamplesPerSecond;

            var f = new System.IO.FileStream(filename, System.IO.FileMode.Create);
            using var wr = new System.IO.BinaryWriter(f);

            wr.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            wr.Write(38 + numsamples * numchannels * samplelength);
            wr.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            wr.Write(16);
            wr.Write((ushort)1);
            wr.Write(numchannels);
            wr.Write(samplerate);
            wr.Write(samplerate * samplelength * numchannels);
            wr.Write((ushort)(samplelength * numchannels));
            wr.Write((ushort)(8 * samplelength));
            wr.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            wr.Write(numsamples * samplelength * numchannels);

            for (int i = 0; i < numsamples; i++)
            {
                wr.Write((byte)((data[i] + (samplelength == 1 ? 128 : 0)) & 0xff));
            }
        }
    }
}
