using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class Place
    {
        public byte[] Data { get; set; } // 32 bytes
        public string Name { get; set; }
    }

    public class Places
    {
        public List<Place> Entries { get; } = new List<Place>();

        private Places()
        {

        }

        public static Places Load(IPlacesReader placesReader, IDataReader dataReader)
        {
            var places = new Places();

            placesReader.ReadPlaces(places, dataReader);

            return places;
        }
    }
}
