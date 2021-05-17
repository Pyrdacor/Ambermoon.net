namespace SonicArranger
{
	public class InstrumentTable
	{
		public int Count { get; private set; }
		public Instrument[] Instruments { get; private set; }

		internal InstrumentTable(ICustomReader reader)
		{
			Count = reader.ReadBEInt32();
			Instruments = new Instrument[Count];
			for (int i = 0; i < Count; i++)
			{
				Instruments[i] = new Instrument(reader);
			}
		}
	}
}
