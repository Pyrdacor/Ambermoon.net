namespace SonicArranger
{
	public struct Arpeggiato
	{
		public byte Length { get; private set; }
		public byte Repeat { get; private set; }
		public byte[] Data { get; private set; }

		internal Arpeggiato(ICustomReader reader) : this()
		{
			Length = reader.ReadByte();
			Repeat = reader.ReadByte();
			Data = reader.ReadBytes(14);
		}
	}
}
