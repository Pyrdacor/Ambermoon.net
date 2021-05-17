namespace SonicArranger
{
	public struct Note
	{
		public byte Value { get; private set; }
		public byte Instrument { get; private set; }
		public byte Command { get; private set; }
		public byte CommandInfo { get; private set; }

		internal Note(ICustomReader reader) : this()
		{
			Value = reader.ReadByte();
			Instrument = reader.ReadByte();
			Command = reader.ReadByte();
			CommandInfo = reader.ReadByte();
		}
	}
}
