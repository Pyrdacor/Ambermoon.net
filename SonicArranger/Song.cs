namespace SonicArranger
{
	public struct Song
	{
		public short SongSpeed { get; private set; }
		public short PatternLength { get; private set; }
		public short StartPos { get; private set; }
		public short StopPos { get; private set; }
		public short RepeatPos { get; private set; }
		public short NBIrqps { get; private set; }

		/// <summary>
		/// This is the BPM when song speed is set to 6 (default).
		/// </summary>
		int baseBPM => 60 * NBIrqps / 24;

		/// <summary>
		/// Initial beats per minute.
		/// </summary>
		public int InitialBPM => GetBPM(SongSpeed);

		public int GetBPM(int songSpeed) => songSpeed == 0 ? 0 : baseBPM * 6 / songSpeed;

		public double GetNotesPerSecond(int songSpeed) => songSpeed == 0 ? 0.0 : (double)NBIrqps / songSpeed;

		public double GetNoteDuration(int songSpeed) => NBIrqps == 0 ? 0.0 : (double)songSpeed / NBIrqps;

		internal Song(ICustomReader reader) : this()
		{
			SongSpeed = reader.ReadBEInt16();
			PatternLength = reader.ReadBEInt16();
			StartPos = reader.ReadBEInt16();
			StopPos = reader.ReadBEInt16();
			RepeatPos = reader.ReadBEInt16();
			NBIrqps = reader.ReadBEInt16();
		}
	}
}
