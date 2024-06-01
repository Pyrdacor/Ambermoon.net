using Ambermoon;
using Ambermoon.Data.Audio;
using Android.Content;
using Android.Media;
using Song = Ambermoon.Data.Enumerations.Song;

namespace AmbermoonAndroid
{
	class MusicManager : ISongManager, IAudioOutput
    {
        const Song PyrdacorSong = (Song)127;

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

		private readonly Context context;
		private MediaPlayer mediaPlayer;
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

        public float Volume
        {
            get => volume;
            set
            {
                volume = Util.Limit(0.0f, value, 1.0f);
				mediaPlayer?.SetVolume(volume, volume);
			}
        }

		public MusicManager(Context context)
        {
            this.context = context;
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

			mediaPlayer = MediaPlayer.Create(context, songIds[song]);
			mediaPlayer.Looping = true;
			mediaPlayer.SetVolume(Volume, Volume);
			mediaPlayer.Start();			

            currentSong = song;
            Streaming = true;
		}

		public void Stop()
        {
			if (mediaPlayer != null)
			{
				mediaPlayer.Stop();
				mediaPlayer.Release();
				mediaPlayer = null;
			}

			Streaming = false;
			currentSong = null;
        }

		private void Pause()
		{
			if (mediaPlayer != null && mediaPlayer.IsPlaying)
			{
				mediaPlayer.Pause();
			}
		}

		private void Resume()
		{
			if (mediaPlayer != null)
			{
				mediaPlayer.Start();
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
