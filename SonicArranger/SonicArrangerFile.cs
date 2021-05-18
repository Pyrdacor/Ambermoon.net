using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SonicArranger
{
	/// <summary>
	/// Timing: There is the primary timing which is
	/// mostly used for effects and settings. One tick
	/// is 1/96 note. The exact duration can be set
	/// by the BPM (beats per minute) setting. But
	/// "beats" means 1/4 note.
	/// 
	/// Table entries used in patterns normally won't
	/// use the primary ticks as 1/96 is much too short
	/// for a sample to play. Therefore the secondary
	/// timing can be used. It is controlled by the
	/// song speed which is 6 by default but can be
	/// changed during playing the song or globally.
	/// 
	/// The song speed is a factor to use multiple
	/// 1/96 ticks for one slot in the pattern table.
	/// So for example song speed 6 means 6 of those
	/// ticks which means 6 * 1/96 = 1/16 note. Each
	/// pattern table entry will be one 1/16 note.
	/// 
	/// For 125 bpm each 1/4 note has a duration of
	/// 60000 / 125 = 480 ms. So each 1/16 note has
	/// a duration of 120 ms which is then also the
	/// duration of one pattern table entry.
	/// 
	/// Sonic Arranger uses 125 bpm by default.
	/// 
	/// The volume (amplitude) is always in the range
	/// 0x00 (0) to 0x40 (64) which means 0% to 100%.
	/// 
	/// Sonic Arranger supports samples and synthetic
	/// mode (waves). If synthetic mode is off (0) a
	/// sample is used, otherwise (1) a wave from the
	/// synth wave tables.
	/// 
	/// Each instrument sets this option in <see cref="Instrument.SynthMode"/>.
	/// Then the sample or synth wave index is given in <see cref="Instrument.SampleWaveNo"/>.
	/// 
	/// Synth waves are given as a wave table and are always 128 bytes long.
	/// But the length can be lower than that and parts can be repeated.
	/// You can access the wave table data by the property <see cref="Waves"/>.
	/// Synth waves are often used together with ADSR waves to create effects.
	/// 
	/// Samples are at the end of the SA file and are stored in <see cref="Samples"/>.
	/// 
	/// There are also two additional wave tables. The first one (right after the
	/// synth wave tables) seem to be the ADSR waves (also up to 128 bytes each).
	/// An instrument can use them by specify the index in <see cref="Instrument.AdsrWave"/>.
	/// Then with <see cref="Instrument.AdsrLength"/> and <see cref="Instrument.AdsrRepeat"/>
	/// the used part can be specified. A length of 0 means that no ADSR is used.
	/// You can access the ADSR waves with <see cref="AdsrWaves"/>.
	/// ADSR wave tables seem to contain volume amplitudes in the range 0 to 64 (100%).
	/// 
	/// The second of those wave tables is stored as "SYAF" and I guess it is related
	/// to the AMF values of the instrument. It is rarely used and I don't know the exact
	/// usage but data-wise it is handled in the same fashion as the ADSR waves.
	/// You can access the ADSR waves with <see cref="AmfWaves"/>.
	/// 
	/// </summary>
	public class SonicArrangerFile
	{
		/// <summary>
		/// Samples seems to be recorded as F-3 (period value = 160).
		/// The sample rate formula of protracker is: 7093789.2 / (period * 2)
		/// 
		/// So it should be 7093789.2 / 320 = 22168.0913.
		/// </summary>
		public const int SampleRate = 22168;

		const double FreqF = 21.83;
		static readonly double[] BaseNoteFactors = new double[12]
		{
			// Recorded as F (21.83 Hz)
			16.35 / FreqF, // C
			17.32 / FreqF, // C#/Db
			18.35 / FreqF, // D
			19.45 / FreqF, // D#/Eb
			20.60 / FreqF, // E
			1.0, // F
			23.12 / FreqF, // F#/Gb
			24.50 / FreqF, // G
			25.96 / FreqF, // G#/Ab
			27.50 / FreqF, // A
			29.14 / FreqF, // A#/Bb
			30.87 / FreqF, // B
		};

		/// <summary>
		/// Gets the frequency factor of a note for
		/// sampled data. The note index is 0-based so
		/// use Note.Value - 1 here.
		/// 
		/// The fine tuning value should be in the range
		/// -8 to +7 and states the amount of semi-tone
		/// increases (-8/8 semi-tones to +7/8 semi-tones).
		/// </summary>
		public static double GetNoteFrequencyFactor(int noteIndex, int fineTuning)
        {
			int octave = noteIndex / 12;
			int note = noteIndex % 12;
			double frequencyFactor = BaseNoteFactors[note];

			if (fineTuning < 0)
            {
				double lower = note == 0 ?
					0.5 * BaseNoteFactors[11] : BaseNoteFactors[note - 1];
				frequencyFactor += (fineTuning / 8.0) * (frequencyFactor - lower);
			}
			else if (fineTuning > 0)
            {
				double upper = note == 11 ?
					2.0 * BaseNoteFactors[0] : BaseNoteFactors[note + 1];
				frequencyFactor += (fineTuning / 8.0) * (upper - frequencyFactor);
			}

			return frequencyFactor * Math.Pow(2.0, octave - 6);
        }

		public string Author { get; private set; }
		public string Version { get; private set; }

		public Song[] Songs { get; private set; }
		public Voice[] Voices { get; private set; }
		public Note[] Notes { get; private set; }
		public Instrument[] Instruments { get; private set; }
		public WaveTable[] Waves { get; private set; }
		public WaveTable[] AmfWaves { get; private set; }
		public WaveTable[] AdsrWaves { get; private set; }
		public Sample[] Samples { get; private set; }

		public SonicArrangerFile(ICustomReader reader)
        {
			void ThrowInvalidData() => throw new InvalidDataException("No valid SonicArranger data stream");

			if (reader.Size < 4)
				ThrowInvalidData();

			string soar = new string(reader.ReadChars(4));

			if (soar != "SOAR")
			{
				reader.Position -= 4;
				int start = FindStart(reader);

				if (start == -1)
					ThrowInvalidData();

				// Songtable
				int songOffset = 0x28;
				// Overtable (Voices)
				int overTableOffset = reader.ReadBEInt32();
				// Notetable
				int noteTableOffset = reader.ReadBEInt32();
				// Instruments
				int instrumentsOffset = reader.ReadBEInt32();
				// Synth waveforms
				int sywtptr = reader.ReadBEInt32();
				// Synth arrangements (ASDR waves?)
				int syarOffset = reader.ReadBEInt32();
				// Synth AF?
				int syafOffset = reader.ReadBEInt32();
				// Sample data
				int samplesOffset = reader.ReadBEInt32();

				ushort magic = reader.ReadBEUInt16(); // always 0x2144 or 0x2154

				if (magic != 0x2144 && magic != 0x2154)
					ThrowInvalidData();

				if (reader.ReadBEUInt16() != 0xffff) // always 0xffff
					ThrowInvalidData();

				uint unknownDword = reader.ReadBEUInt32(); // always 0? end of header marker?

				// Read songs
				reader.Position = start + songOffset;
				int numSongs = (overTableOffset - songOffset) / 12;
				Songs = new Song[numSongs];
				for (int i = 0; i < numSongs; ++i)
				{
					Songs[i] = new Song(reader);
				}

				// Read voices
				reader.Position = start + overTableOffset;
				int numVoices = (noteTableOffset - overTableOffset) / 4;
				Voices = new Voice[numVoices];
				for (int i = 0; i < numVoices; ++i)
				{
					Voices[i] = new Voice(reader);
				}

				// Read notes
				reader.Position = start + noteTableOffset;
				int numNotes = (instrumentsOffset - noteTableOffset) / 4;
				Notes = new Note[numNotes];
				for (int i = 0; i < numNotes; ++i)
				{
					Notes[i] = new Note(reader);
				}

				// Read instruments
				reader.Position = start + instrumentsOffset;
				int numInstruments = (sywtptr - instrumentsOffset) / 152;
				Instruments = new Instrument[numInstruments];
				for (int i = 0; i < numInstruments; ++i)
				{
					Instruments[i] = new Instrument(reader);
				}

				// Read wave forms
				reader.Position = start + sywtptr;
				int numWaveForms = (syarOffset - sywtptr) / 128;
				Waves = new WaveTable[numWaveForms];
				for (int i = 0; i < numWaveForms; ++i)
                {
					Waves[i] = new WaveTable(reader);
                }

				// Read wave forms
				reader.Position = start + syarOffset;
				int numSynthArrangements = (syafOffset - syarOffset) / 128;
				AdsrWaves = new WaveTable[numSynthArrangements];
				for (int i = 0; i < numSynthArrangements; ++i)
				{
					AdsrWaves[i] = new WaveTable(reader);
				}

				// Read wave forms
				reader.Position = start + syafOffset;
				int numSynthAmfWaves = (samplesOffset - syafOffset) / 128;
				AmfWaves = new WaveTable[numSynthAmfWaves];
				for (int i = 0; i < numSynthAmfWaves; ++i)
				{
					AmfWaves[i] = new WaveTable(reader);
				}

				// Read samples
				reader.Position = start + samplesOffset;
				Samples = new SampleTable(reader).Samples;

				if (new string(reader.ReadChars(8)) != "deadbeef" ||
					reader.ReadBEUInt32() != 0)
					ThrowInvalidData();

				List<byte> authorBytes = new List<byte>(256);
				bool readAuthor = true;

				while (true)
                {
					byte b = reader.ReadByte();

					if (b == 0)
						break;

					if (!readAuthor)
						continue;

					// As characters are "NOT" encoded and ASCII is used
					// where the msb is 0, the msb for printable characters
					// should be 1 in the author data. So we stop if this
					// is no longer the case. But we will still wait for
					// the end-marker (0 byte).
					if ((b & 0x80) == 0)
					{
						readAuthor = false;
					}
					else
					{
						// Characters are "NOT" encoded
						b = unchecked((byte)~b);
						authorBytes.Add(b);
					}
				}

				Author = Encoding.ASCII.GetString(authorBytes.ToArray());
				Version = "V1.0";
			}
			else
			{
				Version = new string(reader.ReadChars(4));
				string tag;
				while (true)
				{
					tag = new string(reader.ReadChars(4));
					switch (tag)
					{
						case "STBL":
							Songs = new SongTable(reader).Songs;
							break;
						case "OVTB":
							Voices = new OverTable(reader).Voices;
							break;
						case "NTBL":
							Notes = new NoteTable(reader).Notes;
							break;
						case "INST":
							Instruments = new InstrumentTable(reader).Instruments;
							break;
						default:
							return;
					}
				}
			}
		}

		public SonicArrangerFile(BinaryReader reader)
			: this(new BuiltinReader(reader))
        {

        }

		public SonicArrangerFile(Stream stream, bool leaveOpen = false)
			: this(new BinaryReader(stream, Encoding.ASCII, leaveOpen))
		{
		
		}

		private static int FindStart(ICustomReader reader)
		{
			const uint start = 0x00000028;

			try
			{
				uint check = reader.ReadBEUInt32();

				if (check == start)
					return reader.Position - 4;

				while (reader.Position < reader.Size)
				{
					check <<= 8;
					check |= reader.ReadByte();

					if (check == start)
						return reader.Position - 4;
				}

				return -1;
			}
			catch (EndOfStreamException)
			{
				return -1;
			}
		}

		public static SonicArrangerFile Open(string file)
		{
			using (var stream = new FileStream(file, FileMode.Open))
			{
				return new SonicArrangerFile(stream);
			}
		}
	}
}