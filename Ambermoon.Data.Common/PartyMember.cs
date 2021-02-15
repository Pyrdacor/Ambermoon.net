using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    public class PartyMember : Character, IConversationPartner
    {
        public ushort MarkOfReturnMapIndex { get; set; }
        public ushort MarkOfReturnX { get; set; }
        public ushort MarkOfReturnY { get; set; }
        public List<string> Texts { get; set; }
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();
        public uint MaxWeight => Attributes[Attribute.Strength].TotalCurrentValue * 1000;
        public uint MaxGoldToTake => (uint)Math.Max(0, Math.Min(ushort.MaxValue - Gold, ((int)MaxWeight - (int)TotalWeight) / 5));
        public uint MaxFoodToTake => (uint)Math.Max(0, Math.Min(ushort.MaxValue - Food, ((int)MaxWeight - (int)TotalWeight) / 250));
        public bool CanTakeItems(IItemManager itemManager, ItemSlot itemSlot)
        {
            var item = itemManager.GetItem(itemSlot.ItemIndex);

            // First test if we can carry that much weight.
            uint weight = (uint)itemSlot.Amount * item.Weight;

            if (TotalWeight + weight > MaxWeight)
                return false;

            // Then test if we have enough inventory slots to store the items.
            if (!Inventory.Slots.Any(s => s.Empty))
            {
                if (!item.Flags.HasFlag(ItemFlags.Stackable))
                    return false;

                // If no slot is empty but the item is stackable we check
                // if there are slots with the same item and look if the
                // items would fit into these slots.
                int remainingCount = itemSlot.Amount;

                foreach (var slot in Inventory.Slots.Where(s => s.ItemIndex == itemSlot.ItemIndex))
                    remainingCount -= (99 - slot.Amount);

                if (remainingCount > 0)
                    return false;
            }

            return true;
        }

        private PartyMember()
            : base(CharacterType.PartyMember)
        {

        }

        public static PartyMember Load(uint index, IPartyMemberReader partyMemberReader,
            IDataReader dataReader, IDataReader partyTextReader)
        {
            var partyMember = new PartyMember
            {
                Index = index
            };

            partyMemberReader.ReadPartyMember(partyMember, dataReader, partyTextReader);

            return partyMember;
        }

        public bool HasAmmunition(IItemManager itemManager, AmmunitionType ammunitionType)
        {
            var ammunitionSlot = Equipment.Slots[EquipmentSlot.LeftHand];

            if (ammunitionSlot.Empty)
                return false;

            var ammunition = itemManager.GetItem(ammunitionSlot.ItemIndex);

            return ammunition.Type == ItemType.Ammunition && ammunition.AmmunitionType == ammunitionType;
        }

        public bool HasWorkingWeapon(IItemManager itemManager)
        {
            var weaponSlot = Equipment.Slots[EquipmentSlot.RightHand];

            if (weaponSlot.Empty || weaponSlot.ItemIndex == 0 || weaponSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                return false;

            var weapon = itemManager.GetItem(weaponSlot.ItemIndex);

            if (weapon.Type == ItemType.LongRangeWeapon)
                return HasAmmunition(itemManager, weapon.UsedAmmunitionType);

            return true;
        }

        public void AddGold(uint gold)
        {
            var newGold = (ushort)Math.Min(ushort.MaxValue, Gold + gold);
            TotalWeight += (uint)(newGold - Gold) * 5;
            Gold = newGold;
        }

        public void AddFood(uint food)
        {
            var newFood = (ushort)Math.Min(ushort.MaxValue, Food + food);
            TotalWeight += (uint)(newFood - Food) * 250;
            Food = newFood;
        }

        public void RemoveGold(uint gold)
        {
            var newGold = (ushort)Math.Max(0, Gold - (int)gold);
            TotalWeight -= (uint)(Gold - newGold) * 5;
            Gold = newGold;
        }

        public void RemoveFood(uint food)
        {
            var newFood = (ushort)Math.Max(0, Food - (int)food);
            TotalWeight -= (uint)(Food - newFood) * 250;
            Food = newFood;
        }

        public void SetGold(uint gold)
        {
            RemoveGold(Gold);
            AddGold(gold);
        }

        public void SetFood(uint food)
        {
            RemoveFood(Food);
            AddFood(food);
        }
    }
}
