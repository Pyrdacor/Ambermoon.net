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
		/// Beats per minute.
		/// 
		/// Note: Note 100% sure about the formula.
		/// </summary>
		public double BPM => BPMFromSpeed(SongSpeed);

		/// <summary>
		/// Calculates the BPM from a given song speed.
		/// 
		/// Note: Note 100% sure about the formula.
		/// </summary>
		public double BPMFromSpeed(short speed) => 3000 / speed;//(60.0 * NBIrqps) / (speed * 4);

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
