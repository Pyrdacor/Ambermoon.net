using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class Place
    {
        public byte[] Data { get; set; } // 32 bytes
        public string Name { get; set; }

        protected int GetWord(int offset) => (Data[offset] << 8) | Data[offset + 1];
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

        public class Trainer : Place
        {
            public Ability Ability => (Ability)GetWord(0);
            public int Cost => GetWord(2);
        }

        public class Healer : Place
        {
            // TODO
        }

        public class Sage : Place
        {
            public int Cost => GetWord(0);
        }

        public class Enchanter : Place
        {
            public int Cost => GetWord(0);
        }

        public class Inn : Place
        {
            // TODO
            public int Cost => GetWord(0);
            public int Healing => GetWord(8); // in percent
        }

        public class Merchant : Place
        {
            // No data
        }

        public class FoodDealer : Place
        {
            public int Cost => GetWord(0);
        }

        public class Library : Place
        {
            // No data
        }

        public class ShipDealer : Place
        {
            // TODO
        }

        public class HorseDealer : Place
        {
            // TODO
        }

        public class Blacksmith : Place
        {
            public int Cost => GetWord(0);
        }
    }
}
