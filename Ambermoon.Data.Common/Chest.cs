using System.Linq;

namespace Ambermoon.Data
{
    public enum ChestType
    {        
        Pile, // will disappear after full looting, no items can be put back
        Chest // will stay there and new items can be added by the player
    }

    public class Chest
    {
        public ChestType Type { get; set; }
        public ItemSlot[,] Slots { get; } = new ItemSlot[6, 4];
        public uint Gold { get; set; }
        public uint Food { get; set; }

        public bool Empty => Gold == 0 && Food == 0 && !Slots.Cast<ItemSlot>().Any(s => s.Amount != 0);

        public static Chest Load(IChestReader chestReader, IDataReader dataReader)
        {
            var chest = new Chest();

            chestReader.ReadChest(chest, dataReader);

            return chest;
        }
    }
}
