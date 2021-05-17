namespace SonicArranger
{
	public struct Voice
	{
		public short NoteAddress { get; private set; }
		public byte SoundTranspose { get; private set; }
		public byte NoteTranspose { get; private set; }

		internal Voice(ICustomReader reader) : this()
		{
			NoteAddress = reader.ReadBEInt16();
			SoundTranspose = reader.ReadByte();
			NoteTranspose = reader.ReadByte();
		}
	}
}
