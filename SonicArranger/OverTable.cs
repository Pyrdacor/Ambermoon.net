namespace SonicArranger
{
	public class OverTable
	{
		public int Count { get; private set; }
		public Voice[] Voices { get; private set; }

		internal OverTable(ICustomReader reader)
		{
			Count = reader.ReadBEInt32();
			Voices = new Voice[Count * 4];
			for (int i = 0; i < Count * 4; i++)
			{
				Voices[i] = new Voice(reader);
			}
		}
	}

}
