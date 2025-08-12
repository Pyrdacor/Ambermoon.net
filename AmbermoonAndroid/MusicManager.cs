using Ambermoon;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Android.Content;
using Android.Media;
using Android.OS;
using SonicArranger;
using System.IO.Compression;
using Song = Ambermoon.Data.Enumerations.Song;

namespace AmbermoonAndroid
{
	class MusicManager : ISongManager, IAudioOutput
    {
        const Song PyrdacorSong = (Song)127;

		private static byte[] Decompress(System.IO.Stream compressedStream)
		{
			using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
			using var resultStream = new MemoryStream();
			deflateStream.CopyTo(resultStream);
			return resultStream.ToArray();
		}

		class MusicStream : MemoryStream
		{
			private const int BufferSize = 44100 * 2;
			private readonly SonicArranger.Stream saStream;
			private readonly List<byte> initialBuffer;
			private List<byte> buffer = [];			
			private bool reset = true;
			private bool resetting = false;

			public MusicStream(SonicArranger.Stream saStream)
			{
				this.saStream = saStream;
				initialBuffer = GetBuffer(BufferSize * 2);
				this.buffer.AddRange(initialBuffer);
				reset = true;
			}

			private List<byte> GetBuffer(int minSize)
			{
				var buffer = new List<byte>();
				int remainingCount = minSize;

				while (remainingCount > 0)
				{
					var audioData = saStream.ReadUnsigned(1000, true);
					buffer.AddRange(audioData);
					remainingCount -= audioData.Length;
				}

				return buffer;
			}

			public void Reset()
			{
				if (reset || resetting)
					return;

				lock (this.buffer)
				{
					this.buffer.Clear();
					this.buffer.AddRange(initialBuffer);
					reset = true;
				}

				resetting = true;

				Task.Run(() =>
				{
					saStream.Reset();
					GetBuffer(BufferSize * 2); // only called to align the underlying stream
					resetting = false;
				});
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				lock (this.buffer)
				{
					reset = false;
					return InternalRead(buffer, offset, count);
				}
			}

			protected int InternalRead(byte[] buffer, int offset, int count)
			{
				void EnsureBufferSize(int size)
				{
					while (this.buffer.Count < size)
						this.buffer.AddRange(saStream.ReadUnsigned(1000, true));
				}

				if (this.buffer.Count >= count)
				{
					Array.Copy(this.buffer.ToArray(), 0, buffer, offset, count);
					this.buffer = new(this.buffer.Skip(count));
					EnsureBufferSize(BufferSize);
					return count;
				}

				int remainingCount = count;

				if (this.buffer.Count != 0)
				{
					Array.Copy(this.buffer.ToArray(), 0, buffer, offset, this.buffer.Count);
					offset += this.buffer.Count;
					remainingCount -= this.buffer.Count;
					this.buffer.Clear();
				}

				while (remainingCount > 0)
				{
					var audioData = saStream.ReadUnsigned(1000, true);
					int useCount = Math.Min(remainingCount, audioData.Length);
					Array.Copy(audioData, 0, buffer, offset, useCount);
					offset += useCount;
					remainingCount -= useCount;

					if (useCount < audioData.Length)
						this.buffer.AddRange(audioData.Skip(useCount));
				}

				EnsureBufferSize(BufferSize);
				return count;
			}
		}

		class SonicArrangerSong(MusicManager musicManager, Song song) : ISong
		{
			private readonly MusicManager musicManager = musicManager;

			public Song Song { get; } = song;

			public TimeSpan? SongDuration => null;

			public void Play(IAudioOutput _)
			{
				musicManager.Play(Song);
			}

			public void Stop()
			{
                musicManager.Stop();
			}
		}

		static readonly Dictionary<Song, SonicArrangerSong> songs = [];
		private readonly Dictionary<Song, MusicStream> songStreams = [];
		private readonly Context context;
		private AudioTrack audioTrack;
        private Song? currentSong = null;
        private float volume = 1.0f;
        private bool enabled = true;

		public bool Available => true;

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (!enabled)
                    Pause();
                else if (currentSong != null)
                    Resume();
            }
        }

        public bool Streaming { get; private set; } = false;

		public bool Paused { get; set; } = false;

        public float Volume
        {
            get => volume;
            set
            {
                volume = Util.Limit(0.0f, value, 1.0f);
				audioTrack?.SetVolume(volume);
			}
        }

		public MusicManager(Context context)
        {
            this.context = context;

			using var stream = FileProvider.GetMusic();

			int ReadWord()
			{
				int value = stream.ReadByte();
				return (value << 8) | stream.ReadByte();
			}

			int ReadLong()
			{
				int value = stream.ReadByte();
				value <<= 8;
				value |= stream.ReadByte();
				value <<= 8;
				value |= stream.ReadByte();
				return (value << 8) | stream.ReadByte();
			}

			int fileCount = ReadWord();
			var fileSizes = Enumerable.Range(0, fileCount).Select(_ => ReadLong()).ToArray();
			var data = Decompress(stream);
			int offset = 0;

			for (int i = 0; i < fileSizes.Length; i++)
			{
				var size = fileSizes[i];
				var saFile = new SonicArrangerFile(new DataReader(data, offset, size));
				offset += size;

				int index = i + 1;

				void AddSong(Song song, int songIndex = 0)
				{
					var stream = new SonicArranger.Stream(saFile, songIndex, 44100, SonicArranger.Stream.ChannelMode.Mono, true, true);
					songStreams.Add(song, new MusicStream(stream));
				}

				if (index < (int)Song.Intro)
				{
					AddSong((Song)index);
				}
				else
				{
					index -= (int)Song.Intro;

					if (index == 0)
						AddSong(Song.Outro);
					else if (index == 2)
						AddSong(PyrdacorSong);
					else
					{
						AddSong(Song.Intro);
						AddSong(Song.Menu, 1);
					}
				}
			}
		}

		public ISong GetSong(Song index)
		{
			if (songs.TryGetValue(index, out var song))
				return song;

			song = new SonicArrangerSong(this, index);

			songs.Add(index, song);

			return song;
		}

		public ISong GetPyrdacorSong()
		{
			if (songs.TryGetValue(PyrdacorSong, out var song))
				return song;

			song = new SonicArrangerSong(this, PyrdacorSong);

			songs.Add(PyrdacorSong, song);

			return song;
		}

		private System.IO.Stream GetSongStream(Song song, int resourceId)
		{
			var resourceStream = context.Resources.OpenRawResource(resourceId);
			var memoryStream = new MemoryStream();
			resourceStream.CopyTo(memoryStream);
			memoryStream.Position = 0;
			return memoryStream;
		}

		private void Play(Song song)
        {
			if (!Available)
				return;

            if (Streaming && currentSong == song)
                return;

			Stop();

			var musicStream = songStreams[song];
			int bufferSize = AudioTrack.GetMinBufferSize(44100, ChannelOut.Stereo, Encoding.Pcm16bit) * 2;

			var audioFormat = new AudioFormat.Builder()
				.SetEncoding(Encoding.Pcm8bit)
				.SetSampleRate(44100)
				.SetChannelMask(ChannelOut.Mono)
				.Build();
			var audioAttributes = new AudioAttributes.Builder()
				.SetContentType(AudioContentType.Music)
				.Build();
			if (Build.VERSION.SdkInt >= BuildVersionCodes.M) // API level 23 and above
			{
				audioTrack = new AudioTrack.Builder()
					.SetAudioAttributes(audioAttributes)
					.SetAudioFormat(audioFormat)
					.SetBufferSizeInBytes(bufferSize)
					.SetTransferMode(AudioTrackMode.Stream)
					.Build();
			}
			else // Below API level 23
			{
				audioTrack = new AudioTrack(
					// Audio stream type
					Android.Media.Stream.Music,
					// Sample rate in Hz
					audioFormat.SampleRate,
					// Channel configuration
					audioFormat.ChannelMask,
					// Audio format
					audioFormat.Encoding,
					// Buffer size in bytes
					bufferSize,
					// Mode
					AudioTrackMode.Stream);
			}
			audioTrack.SetVolume(Volume);
			audioTrack.Play();

			currentSong = song;
            Streaming = true;
			Paused = false;

			Task.Run(() => StreamAudio(musicStream, bufferSize));
		}

		private void StreamAudio(MusicStream musicStream, int bufferSize)
		{
			var buffer = new byte[bufferSize];
			int offset = 0;
			var threadAudioTrack = audioTrack;

			while (Streaming)
			{
				if (threadAudioTrack != audioTrack)
					return;

				lock (audioTrack)
				{
					if (threadAudioTrack != audioTrack)
						return;

					while (Paused)
						Monitor.Wait(audioTrack);

					if (threadAudioTrack != audioTrack)
						return;
				}

				if (!Streaming)
					break;

				if (threadAudioTrack != audioTrack)
					return;

				lock (audioTrack)
				{
					if (threadAudioTrack != audioTrack)
						return;

					int readCount = musicStream.Read(buffer, 0, bufferSize);

					if (threadAudioTrack != audioTrack)
						return;

					audioTrack.Write(buffer, offset, readCount);
				}

				try
				{
					Thread.Sleep(10);
				}
				catch (ThreadInterruptedException) { }
			}
		}

		public void Stop()
        {
			if (audioTrack != null)
			{
				lock (audioTrack)
				{
					Streaming = false;
					Paused = false;
					Monitor.Pulse(audioTrack);
					audioTrack.Stop();
					audioTrack.Release();
					audioTrack = null;
				}
			}

			if (currentSong is not null)
				songStreams[currentSong.Value]?.Reset();

			currentSong = null;
		}

		private void Pause()
		{
			if (audioTrack != null && !Paused && Streaming)
			{
				lock (audioTrack)
				{
					Paused = true;
					audioTrack.Pause();
				}
			}
		}

		private void Resume()
		{
			if (audioTrack != null && Paused)
			{
				lock (audioTrack)
				{
					Paused = false;
					audioTrack.Play();
					Monitor.Pulse(audioTrack);
				}
			}
		}

		// We don't need the following. This is just to fulfill the IAudioOutput contract
		// but the songs won't call those methods.
		public void Start()
		{
			throw new NotImplementedException();
		}

		public void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, bool sample8Bit = true)
		{
			throw new NotImplementedException();
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}
	}
}
