namespace SonicArranger
{
	public struct Instrument
	{
		/// <summary>
		/// Synthetic mode (off/on).
		/// </summary>
		public bool SynthMode { get; private set; }
		/// <summary>
		/// 0-based sample or wave index (dependent on <see cref="SynthMode"/>).
		/// </summary>
		public short SampleWaveNo { get; private set; }
		/// <summary>
		/// Length in words (max 64 for synthetic instruments).
		/// </summary>
		public short Length { get; private set; }
		/// <summary>
		/// Repeat size in words.
		/// 
		/// Always 0 for synthetic instruments.
		/// 
		/// Note: The total size in bytes for sampled instruments is
		/// <see cref="Length"/> * 2 + <see cref="Repeat"/> * 2.
		/// </summary>
		public short Repeat { get; private set; }
		//8
		/// <summary>
		/// Volume level (0 to 64).
		/// </summary>
		public short Volume { get; private set; }
		/// <summary>
		/// 0 to 255.
		/// </summary>
		public short FineTuning { get; private set; }
		public short Portamento { get; private set; }
		/// <summary>
		/// 255 (or -1) means no vibrato (default).
		/// 
		/// Range can thus be 0 to 254.
		/// </summary>
		public short VibDelay { get; private set; }
		/// <summary>
		/// Default value is 18 (0x12). Range is 0 to 255.
		/// </summary>
		public short VibSpeed { get; private set; }
		/// <summary>
		/// Default value is 160 (0xA0). Range is 0 to 255.
		/// </summary>
		public short VibLevel { get; private set; }
		/// <summary>
		/// 0-based AMF wave index.
		/// </summary>
		public short AmfWave { get; private set; }
		/// <summary>
		/// Default value is 1. Range is 1 to 255.
		/// </summary>
		public short AmfDelay { get; private set; }
		/// <summary>
		/// Size of AMF wave data in bytes.
		/// 
		/// Note: The total size is <see cref="AmfLength"/> + <see cref="AmfRepeat"/>.
		/// </summary>
		public short AmfLength { get; private set; }
		/// <summary>
		/// Size of the repeat portion of the AMF wave data in bytes.
		/// 
		/// Note: The total size is <see cref="AmfLength"/> + <see cref="AmfRepeat"/>.
		/// </summary>
		public short AmfRepeat { get; private set; }
		/// <summary>
		/// 0-based ADSR wave index.
		/// </summary>
		public short AdsrWave { get; private set; }
		/// <summary>
		/// Default value is 1. Range is 1 to 255.
		/// </summary>
		public short AdsrDelay { get; private set; }
		/// <summary>
		/// Size of ADSR wave data in bytes.
		/// 
		/// Note: The total size is <see cref="AdsrLength"/> + <see cref="AdsrRepeat"/>.
		/// </summary>
		public short AdsrLength { get; private set; }
		/// <summary>
		/// Size of the repeat portion of the ADSR wave data in bytes.
		/// 
		/// Note: The total size is <see cref="AdsrLength"/> + <see cref="AdsrRepeat"/>.
		/// </summary>
		public short AdsrRepeat { get; private set; }
		/// <summary>
		/// The 0-based index of the ADSR wave data byte
		/// to use as the sustain.
		/// </summary>
		public short SustainPt { get; private set; }
		public short SustainVal { get; private set; }
		//16
		public short EffectNumber { get; private set; }
		public short Effect1 { get; private set; }
		public short Effect2 { get; private set; }
		public short Effect3 { get; private set; }
		public short EffectDelay { get; private set; }

		public Arpeggiato[] ArpegData { get; private set; }

		public string Name { get; private set; }

		internal Instrument(ICustomReader reader) : this()
		{
			SynthMode = reader.ReadBEInt16() != 0;
			SampleWaveNo = reader.ReadBEInt16();
			Length = reader.ReadBEInt16();
			Repeat = reader.ReadBEInt16();
			reader.ReadBytes(8);
			Volume = reader.ReadBEInt16();
			FineTuning = reader.ReadBEInt16();
			Portamento = reader.ReadBEInt16();
			VibDelay = reader.ReadBEInt16();
			if (VibDelay >= 255)
				VibDelay = -1;
			VibSpeed = reader.ReadBEInt16();
			VibLevel = reader.ReadBEInt16();
			AmfWave = reader.ReadBEInt16();
			AmfDelay = reader.ReadBEInt16();
			AmfLength = reader.ReadBEInt16();
			AmfRepeat = reader.ReadBEInt16();
			AdsrWave = reader.ReadBEInt16();
			AdsrDelay = reader.ReadBEInt16();
			AdsrLength = reader.ReadBEInt16();
			AdsrRepeat = reader.ReadBEInt16();
			SustainPt = reader.ReadBEInt16();
			SustainVal = reader.ReadBEInt16();
			reader.ReadBytes(16);
			EffectNumber = reader.ReadBEInt16();
			Effect1 = reader.ReadBEInt16();
			Effect2 = reader.ReadBEInt16();
			Effect3 = reader.ReadBEInt16();
			EffectDelay = reader.ReadBEInt16();

			ArpegData = new Arpeggiato[3];
			for (int i = 0; i < ArpegData.Length; i++)
			{
				ArpegData[i] = new Arpeggiato(reader);
			}
			Name = new string(reader.ReadChars(30)).Split(new[] { '\0' }, 2)[0];
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
