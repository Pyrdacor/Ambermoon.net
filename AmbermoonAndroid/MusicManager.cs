using Ambermoon;
using Ambermoon.Data.Audio;
using Android.Content;
using Android.Media;
using Android.OS;
using MP3Sharp;
using Song = Ambermoon.Data.Enumerations.Song;

namespace AmbermoonAndroid
{
	class MusicManager : ISongManager, IAudioOutput
    {
        const Song PyrdacorSong = (Song)127;

		class MusicStream : MemoryStream
		{
			private readonly MP3Stream mp3Stream;
			private readonly MemoryStream buffer = new();
			private bool fullyLoaded = false;

			public int Frequency { get; }

			public MusicStream(MP3Stream mp3Stream)
			{
				this.mp3Stream = mp3Stream;
				Frequency = mp3Stream.Frequency;
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (fullyLoaded)
				{
					int tail = Math.Min(count, (int)(this.buffer.Length - this.buffer.Position));
					int head = count - tail;
					int readCount = this.buffer.Read(buffer, offset, tail);

					if (head > 0)
					{
						this.buffer.Position = 0;
						readCount += this.buffer.Read(buffer, offset + tail, head);
					}
					else if (this.buffer.Position == this.buffer.Length)
					{
						this.buffer.Position = 0;
					}

					return readCount;
				}
				else
				{
					int readCount = 0;

					if (!mp3Stream.IsEOF)
					{
						try
						{
							readCount = mp3Stream.Read(buffer, offset, count);
							this.buffer.Write(buffer, 0, readCount);

							if (readCount < count)
							{
								fullyLoaded = true;
								this.buffer.Position = 0;
								mp3Stream.Close();
								readCount += Read(buffer, offset + readCount, count - readCount);
							}

							return readCount;
						}
						catch (Exception ex)
						{
							fullyLoaded = true;
							this.buffer.Position = 0;
							mp3Stream.Close();
							return Read(buffer, offset, count);
						}
					}
					else
					{
						fullyLoaded = true;
						this.buffer.Position = 0;
						mp3Stream.Close();
						return Read(buffer, offset, count);
					}
				}
			}
		}

		class Mp3Song : ISong
		{
			private readonly MusicManager musicManager;

			public Mp3Song(MusicManager musicManager, Song song)
			{
				this.musicManager = musicManager;
				Song = song;
			}

			public Song Song { get; }

			public TimeSpan? SongDuration => musicManager.GetSongDuration(Song);

			public void Play(IAudioOutput _)
			{
				musicManager.Play(Song);
			}

			public void Stop()
			{
                musicManager.Stop();
			}
		}

		static readonly Dictionary<Song, int> songIds = new()
		{
			{ Song.WhoSaidHiHo, Resource.Raw.sonic_whosaidhiho },
            { Song.MellowCamelFunk, Resource.Raw.sonic_mellowcamelfunk },
            { Song.CloseToTheHedge, Resource.Raw.sonic_closethehedge },
            { Song.VoiceOfTheBagpipe, Resource.Raw.sonic_voiceofthebagpipe },
            { Song.Downtown, Resource.Raw.sonic_downtown },
            { Song.Ship, Resource.Raw.sonic_ship },
            { Song.WholeLottaDove, Resource.Raw.sonic_wholelottadove },
            { Song.HorseIsNoDisgrace, Resource.Raw.sonic_horseisnodisgrace },
            { Song.DontLookBach, Resource.Raw.sonic_dontlookbach },
            { Song.RoughWaterfrontTavern, Resource.Raw.sonic_roughwaterfronttavern },
            { Song.SapphireFireballsOfPureLove, Resource.Raw.sonic_sapphirefireballsofpurelove },
            { Song.TheAumRemainsTheSame, Resource.Raw.sonic_theaumremainsthesame },
            { Song.Capital, Resource.Raw.sonic_capital },
            { Song.PloddingAlong, Resource.Raw.sonic_ploddingalong },
            { Song.CompactDisc, Resource.Raw.sonic_compactdisc },
            { Song.RiversideTravellingBlues, Resource.Raw.sonic_riversidetravellingblues },
            { Song.NobodysVaultButMine, Resource.Raw.sonic_nobodysvaultbutmine },
            { Song.LaCryptaStrangiato, Resource.Raw.sonic_lacryptastrangiato },
            { Song.MistyDungeonHop, Resource.Raw.sonic_mistydungeonhop },
            { Song.BurnBabyBurn, Resource.Raw.sonic_burnbabyburn },
            { Song.BarBrawlin, Resource.Raw.sonic_barbrawlin },
            { Song.PsychedelicDuneGroove, Resource.Raw.sonic_psychedelicdunegroove },
            { Song.StairwayToLevel50, Resource.Raw.sonic_stairway_to_level_50 },
            { Song.ThatHunchIsBack, Resource.Raw.sonic_thathunchisback },
            { Song.ChickenSoup, Resource.Raw.sonic_chickensoup },
            { Song.DragonChaseInCreepyDungeon, Resource.Raw.sonic_dragonchaseincreepydungeon },
            { Song.HisMastersVoice, Resource.Raw.sonic_hismastersvoice },
            { Song.NoName, Resource.Raw.sonic_nonamesecret },
            { Song.OhNoNotAnotherMagicalEvent, Resource.Raw.sonic_ohnonotanothermagevent },
            { Song.TheUhOhSong, Resource.Raw.sonic_theuhohsong },
            { Song.OwnerOfALonelySword, Resource.Raw.sonic_ownerofalonelysword },
            { Song.GameOver, Resource.Raw.sonic_gameover },
            { Song.Intro, Resource.Raw.sonic_intro },
            { Song.Outro, Resource.Raw.sonic_extro },
            { Song.Menu, Resource.Raw.sonic_mainmenu },
            { PyrdacorSong, Resource.Raw.song }
		};
		static readonly Dictionary<Song, TimeSpan> songDurations = new();
		static readonly Dictionary<Song, Mp3Song> songs = new();

		private readonly Dictionary<Song, MusicStream> songStreams = new();
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

			foreach (var songId in songIds)
			{
				songStreams.Add(songId.Key, new MusicStream(new MP3Stream(GetSongStream(songId.Key, songId.Value))));
			}
        }

		public ISong GetSong(Song index)
		{
			if (songs.TryGetValue(index, out var song))
				return song;

			song = new Mp3Song(this, index);

			songs.Add(index, song);

			return song;
		}

		public ISong GetPyrdacorSong()
		{
			if (songs.TryGetValue(PyrdacorSong, out var song))
				return song;

			song = new Mp3Song(this, PyrdacorSong);

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

		private TimeSpan GetSongDuration(Song song)
        {
            if (songDurations.TryGetValue(song, out var duration))
                return duration;

			MediaPlayer tempMediaPlayer = MediaPlayer.Create(context, songIds[song]);
			duration = TimeSpan.FromMicroseconds(tempMediaPlayer.Duration);
			tempMediaPlayer.Release();

            songDurations.Add(song, duration);

			return duration;
		}

		private void Play(Song song)
        {
            if (Streaming && currentSong == song)
                return;

            Stop();

			var musicStream = songStreams[song];
			int bufferSize = AudioTrack.GetMinBufferSize(musicStream.Frequency, ChannelOut.Stereo, Encoding.Pcm16bit) * 2;

			var audioFormat = new AudioFormat.Builder()
				.SetEncoding(Encoding.Pcm16bit)
				.SetSampleRate(musicStream.Frequency)
				.SetChannelMask(ChannelOut.Stereo)
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

			while (Streaming)
			{
				lock (audioTrack)
				{
					while (Paused)
						Monitor.Wait(audioTrack);
				}

				if (!Streaming)
					break;

				lock (audioTrack)
				{
					int readCount = musicStream.Read(buffer, 0, bufferSize);

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
