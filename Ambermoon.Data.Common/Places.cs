using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IPlace
    {
        uint AvailableGold { get; set; }
        PlaceType PlaceType { get; set; }
    }

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
            public Trainer(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public Ability Ability => (Ability)GetWord(0);
            public int Cost => GetWord(2);
        }

        public class Healer : Place
        {
            public Healer(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            // TODO
            // Most likely 11 words for the costs of all healable ailments
            // starting at Lamed to DeadDust, then Crazy, Blind, Drugged.
            // Then one word for the cost to heal 1 LP.
            // And then maybe a word for the price of removing a curse?
        }

        public class Sage : Place
        {
            public Sage(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
        }

        public class Enchanter : Place
        {
            public Enchanter(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
        }

        public class Inn : Place
        {
            public Inn(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            // TODO
            public int Cost => GetWord(0);
            public int Healing => GetWord(8); // in percent
        }

        public class Merchant : Place
        {
            public Merchant(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            // No data
        }

        public class FoodDealer : Place
        {
            public FoodDealer(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
        }

        public class Library : Place
        {
            public Library(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            // No data
        }

        public class ShipDealer : Place
        {
            public ShipDealer(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
            public int SpawnX => GetWord(2);
            public int SpawnY => GetWord(4);
            public int SpawnMapIndex => GetWord(6);
            public StationaryImage StationaryImage => (StationaryImage)GetWord(8);
        }

        public class HorseDealer : Place
        {
            public HorseDealer(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
            public int SpawnX => GetWord(2);
            public int SpawnY => GetWord(4);
            public int SpawnMapIndex => GetWord(6);
            public StationaryImage StationaryImage => (StationaryImage)GetWord(8);
        }

        public class Blacksmith : Place
        {
            public Blacksmith(Place place)
            {
                Data = place.Data;
                Name = place.Name;
            }

            public int Cost => GetWord(0);
        }
    }
}
