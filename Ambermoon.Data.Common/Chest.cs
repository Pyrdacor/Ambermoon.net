using Ambermoon.Data.Serialization;
using System.Linq;

namespace Ambermoon.Data
{
    public enum ChestType
    {
        /// <summary>
        /// Normal chest. You can put items, gold and rations into it.
        /// </summary>
        Chest,
        /// <summary>
        /// Junk pile or item pickups. You can not put items into it but no gold or rations.
        /// </summary>
        Junk
    }

    public class Chest : ITreasureStorage
    {
        public const int SlotsPerRow = 6;
        public const int SlotRows = 4;

        public ChestType Type { get; set; }
        public ItemSlot[,] Slots { get; } = new ItemSlot[SlotsPerRow, SlotRows];
        public uint Gold { get; set; }
        public uint Food { get; set; }
        public bool AllowsItemDrop { get; set; } = true;
        public bool IsBattleLoot { get; set; } = false;

        public bool Empty => Gold == 0 && Food == 0 && !Slots.Cast<ItemSlot>().Any(s => s.Amount != 0);

        public Chest()
        {

        }

        public static Chest Load(IChestReader chestReader, IDataReader dataReader)
        {
            var chest = new Chest();

            chestReader.ReadChest(chest, dataReader);

            return chest;
        }

        public void ResetItem(int slot, ItemSlot item)
        {
            int column = slot % SlotsPerRow;
            int row = slot / SlotsPerRow;

            if (Slots[column, row].Add(item) != 0)
                throw new AmbermoonException(ExceptionScope.Application, "Unable to reset chest item.");
        }

        public ItemSlot GetSlot(int slot) => Slots[slot % SlotsPerRow, slot / SlotsPerRow];

        public bool Equals(Chest other, bool includeChargesAndFlags = true)
        {
            if (other is null)
                return false;

            if (other == this)
                return true;

            if (Gold != other.Gold || Food != other.Food)
                return false;

            for (int y = 0; y < SlotRows; ++y)
            {
                for (int x = 0; x < SlotsPerRow; ++x)
                {
                    if (!Slots[x, y].Equals(other.Slots[x, y], includeChargesAndFlags))
                        return false;
                }
            }

            return true;
        }
    }
}
