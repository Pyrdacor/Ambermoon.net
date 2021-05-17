namespace SonicArranger
{
	public class SampleTable
	{
		public int Count { get; private set; }
		public Sample[] Samples { get; private set; }

		internal SampleTable(ICustomReader reader)
		{
			Count = reader.ReadBEInt32();
			var sampleSizes = new int[Count];
			for (int i = 0; i < Count; ++i)
				sampleSizes[i] = reader.ReadBEInt32();
			Samples = new Sample[Count];
			for (int i = 0; i < Count; i++)
				Samples[i] = new Sample(reader, sampleSizes[i]);
		}
	}

}
