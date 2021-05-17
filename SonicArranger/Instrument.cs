namespace SonicArranger
{
	public struct Instrument
	{
		public short SynthMode { get; private set; }
		public short SampleWaveNo { get; private set; }
		public short Length { get; private set; }
		public short Repeat { get; private set; }
		//8
		public short Volume { get; private set; }
		public short FineTuning { get; private set; }
		public short Portamento { get; private set; }
		public short VibDelay { get; private set; }
		public short VibSpeed { get; private set; }
		public short VibLevel { get; private set; }
		public short AmfWave { get; private set; }
		public short AmfDelay { get; private set; }
		public short AmfLength { get; private set; }
		public short AmfRepeat { get; private set; }
		public short AdsrWave { get; private set; }
		public short AdsrDelay { get; private set; }
		public short AdsrLength { get; private set; }
		public short AdsrRepeat { get; private set; }
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
			SynthMode = reader.ReadBEInt16();
			SampleWaveNo = reader.ReadBEInt16();
			Length = reader.ReadBEInt16();
			Repeat = reader.ReadBEInt16();
			reader.ReadBytes(8);
			Volume = reader.ReadBEInt16();
			FineTuning = reader.ReadBEInt16();
			Portamento = reader.ReadBEInt16();
			VibDelay = reader.ReadBEInt16();
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
