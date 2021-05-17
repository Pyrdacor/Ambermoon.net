namespace SonicArranger
{
	public class SongTable
	{
		public int Count { get; private set; }
		public Song[] Songs { get; private set; }

		internal SongTable(ICustomReader reader)
		{
			Count = reader.ReadBEInt32();
			Songs = new Song[Count];
			for (int i = 0; i < Count; i++)
			{
				Songs[i] = new Song(reader);
			}
		}
	}
}
