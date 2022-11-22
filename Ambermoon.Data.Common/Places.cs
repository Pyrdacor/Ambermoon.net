using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IPlace : IItemStorage
    {
        string Name { get; }
        uint AvailableGold { get; set; }
        PlaceType PlaceType { get; }
    }

    public abstract class NonItemPlace : IPlace
    {
        readonly Place place;

        protected NonItemPlace(Place place)
        {
            this.place = place;
        }

        public void ResetItem(int slot, ItemSlot item) { }
        public ItemSlot GetSlot(int slot) => null;
        private protected int GetWord(int offset) => place.GetWord(offset);

        public ItemSlot[,] Slots { get; } = new ItemSlot[6, 2];
        public bool AllowsItemDrop
        {
            get => false;
            set { }
        }

        public string Name => place.Name;
        public uint AvailableGold { get; set; }
        public abstract PlaceType PlaceType { get; }
    }

    public class Place
    {
        public byte[] Data { get; set; } // 32 bytes
        public string Name { get; set; }

        internal int GetWord(int offset) => (Data[offset] << 8) | Data[offset + 1];
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

        public class Trainer : NonItemPlace
        {
            public Trainer(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Trainer;
            public Skill Skill => (Skill)GetWord(0);
            public int Cost => GetWord(2);

            public override string ToString()
            {
                return $"{Skill} Trainer, Cost: {Cost}";
            }
        }

        public class Healer : NonItemPlace
        {
            public Healer(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Healer;

            public int HealLamedCost => GetWord(0);
            public int HealPoisonedCost => GetWord(2);
            public int HealPetrifiedCost => GetWord(4);
            public int HealDiseasedCost => GetWord(6);
            public int HealAgingCost => GetWord(8);
            public int HealDeadCorpseCost => GetWord(10);
            public int HealDeadAshesCost => GetWord(12);
            public int HealDeadDustCost => GetWord(14);
            public int HealCrazyCost => GetWord(16);
            public int HealBlindCost => GetWord(18);
            public int HealDruggedCost => GetWord(20);
            public int HealLPCost => GetWord(22);
            public int RemoveCurseCost => GetWord(24);

            public int GetCostForHealingCondition(Condition condition)
            {
                return condition switch
                {
                    Condition.Crazy => HealCrazyCost,
                    Condition.Blind => HealBlindCost,
                    Condition.Drugged => HealDruggedCost,
                    Condition.Lamed => HealLamedCost,
                    Condition.Poisoned => HealPoisonedCost,
                    Condition.Petrified => HealPetrifiedCost,
                    Condition.Diseased => HealDiseasedCost,
                    Condition.Aging => HealAgingCost,
                    Condition.DeadCorpse => HealDeadCorpseCost,
                    Condition.DeadAshes => HealDeadAshesCost,
                    Condition.DeadDust => HealDeadDustCost,
                    _ => 0
                };
            }

            public override string ToString()
            {
                string text = $"Healer, Heal LP: {HealLPCost}, RemoveCurses: {RemoveCurseCost}";

                void AddCondition(Condition condition)
                {
                    text += $", Heal{condition}: {GetCostForHealingCondition(condition)}";
                }

                AddCondition(Condition.Crazy);
                AddCondition(Condition.Blind);
                AddCondition(Condition.Drugged);
                AddCondition(Condition.Lamed);
                AddCondition(Condition.Poisoned);
                AddCondition(Condition.Petrified);
                AddCondition(Condition.Diseased);
                AddCondition(Condition.Aging);
                AddCondition(Condition.DeadCorpse);
                AddCondition(Condition.DeadAshes);
                AddCondition(Condition.DeadDust);


                return text;
            }
        }

        public class Sage : NonItemPlace
        {
            public Sage(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Sage;
            public int IdentificationCost => GetWord(0);
            public int TellingSLPCost => GetWord(2);

            public override string ToString()
            {
                if (TellingSLPCost == 0)
                    return $"Sage, Identification Cost: {IdentificationCost}";

                return $"Sage, Identification Cost: {IdentificationCost}, Tell SLP Cost: {TellingSLPCost}";
            }
        }

        public class Enchanter : NonItemPlace
        {
            public Enchanter(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Enchanter;
            public int Cost => GetWord(0);

            public override string ToString()
            {
                return $"Enchanter, Cost: {Cost}";
            }
        }

        public class Inn : NonItemPlace
        {
            public Inn(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Inn;
            public int Cost => GetWord(0);
            public int BedroomX => GetWord(2);
            public int BedroomY => GetWord(4);
            public int BedroomMapIndex => GetWord(6);

            public int Healing => GetWord(8); // in percent

            public override string ToString()
            {
                return $"Inn, Cost: {Cost}, Healing: {Healing}%, Spawn: Map {BedroomMapIndex} at {BedroomX},{BedroomY}";
            }
        }

        public class FoodDealer : NonItemPlace
        {
            public FoodDealer(Place place)
                : base(place)
            {

            }

            public uint AvailableFood { get; set; }
            public override PlaceType PlaceType => PlaceType.FoodDealer;
            public int Cost => GetWord(0);

            public override string ToString()
            {
                return $"FoodDealer, Cost: {Cost}";
            }
        }

        public class Library : Merchant
        {
            public override PlaceType PlaceType => PlaceType.Library;
        }

        public abstract class Salesman : NonItemPlace
        {
            public Salesman(Place place)
                : base(place)
            {

            }

            public int Cost => GetWord(0);
            public int SpawnX => GetWord(2);
            public int SpawnY => GetWord(4);
            public int SpawnMapIndex => GetWord(6);
            public virtual TravelType TravelType => (TravelType)GetWord(8);

            public override string ToString()
            {
                return $"{TravelType}Dealer, Cost: {Cost}, Spawn: Map {SpawnMapIndex} at {SpawnX},{SpawnY}";
            }
        }

        public class RaftSalesman : Salesman
        {
            public RaftSalesman(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.RaftDealer;
            public override TravelType TravelType => TravelType.Raft;
        }

        public class ShipSalesman : Salesman
        {
            public ShipSalesman(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.ShipDealer;
            public override TravelType TravelType => TravelType.Ship;
        }

        public class HorseSalesman : Salesman
        {
            public HorseSalesman(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.HorseDealer;
            public override TravelType TravelType => TravelType.Horse;
        }

        public class Blacksmith : NonItemPlace
        {
            public Blacksmith(Place place)
                : base(place)
            {

            }

            public override PlaceType PlaceType => PlaceType.Blacksmith;
            public int Cost => GetWord(0);

            public override string ToString()
            {
                return $"Blacksmith, Cost: {Cost}";
            }
        }
    }
}
