using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class PlacesReader : IPlacesReader
    {
        public void ReadPlaces(Places places, IDataReader dataReader)
        {
            places.Entries.Clear();

            int count = dataReader.ReadWord();

            for (int i = 0; i < count; ++i)
            {
                places.Entries.Add(new Place
                {
                    Data = dataReader.ReadBytes(32)
                });
            }

            for (int i = 0; i < count; ++i)
            {
                places.Entries[i].Name = dataReader.ReadString(30).Trim(new char[] { ' ', '\0' });
            }
        }
    }
}
