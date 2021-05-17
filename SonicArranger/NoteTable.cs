namespace SonicArranger
{
	public class NoteTable
	{
		public int Count { get; private set; }
		public Note[] Notes { get; private set; }

		internal NoteTable(ICustomReader reader)
		{
			Count = reader.ReadBEInt32();
			Notes = new Note[Count];
			for (int i = 0; i < Count; i++)
			{
				Notes[i] = new Note(reader);
			}
		}
	}
}
