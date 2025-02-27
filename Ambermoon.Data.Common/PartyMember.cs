using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    public class PartyMember : Character, IConversationPartner
    {
        public int LookAtTextIndex => LookAtCharTextIndex;
        public ushort MarkOfReturnMapIndex { get; set; }
        public ushort MarkOfReturnX { get; set; }
        public ushort MarkOfReturnY { get; set; }
        public byte MaxReachedLevel { get; set; }
        public List<string> Texts { get; set; }
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();
        public uint MaxWeight => 999 + Attributes[Attribute.Strength].TotalCurrentValue * 1000;
        public bool Overweight => (TotalWeight / 1000) > (MaxWeight / 1000);
        public uint MaxGoldToTake => (uint)Math.Max(0, Math.Min(ushort.MaxValue - Gold, ((int)MaxWeight - (int)TotalWeight) / Character.GoldWeight));
        public uint MaxFoodToTake => (uint)Math.Max(0, Math.Min(ushort.MaxValue - Food, ((int)MaxWeight - (int)TotalWeight) / Character.FoodWeight));
        public bool CanTakeItems(IItemManager itemManager, ItemSlot itemSlot)
        {
            var item = itemManager.GetItem(itemSlot.ItemIndex);

            if (Class == Class.Animal && !item.Classes.HasFlag(ClassFlag.Animal))
                return false;

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

        public PartyMember()
            : base(CharacterType.PartyMember)
        {

        }

        public static PartyMember Load(uint index, IPartyMemberReader partyMemberReader,
            IDataReader dataReader, IDataReader partyTextReader, IDataReader fallbackDataReader = null)
        {
            var partyMember = new PartyMember
            {
                Index = index
            };

            partyMemberReader.ReadPartyMember(partyMember, dataReader, partyTextReader, fallbackDataReader);

            return partyMember;
        }

        public override bool CanMove(bool battle = true)
        {
            if (battle)
                return base.CanMove(true);
            else
                return !Overweight;
        }

        public override bool CanFlee()
        {
            return base.CanFlee();
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

        public bool HasItem(uint itemIndex) => Inventory.Slots.Any(s => s.ItemIndex == itemIndex);

        public void AddGold(uint gold)
        {
            var newGold = (ushort)Math.Min(ushort.MaxValue, Gold + gold);
            TotalWeight += (uint)(newGold - Gold) * Character.GoldWeight;
            Gold = newGold;
        }

        public void AddFood(uint food)
        {
            var newFood = (ushort)Math.Min(ushort.MaxValue, Food + food);
            TotalWeight += (uint)(newFood - Food) * Character.FoodWeight;
            Food = newFood;
        }

        public void RemoveGold(uint gold)
        {
            var newGold = (ushort)Math.Max(0, Gold - (int)gold);
            TotalWeight -= (uint)(Gold - newGold) * Character.GoldWeight;
            Gold = newGold;
        }

        public void RemoveFood(uint food)
        {
            var newFood = (ushort)Math.Max(0, Food - (int)food);
            TotalWeight -= (uint)(Food - newFood) * Character.FoodWeight;
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

        public uint GetNextLevelExperiencePoints(Features? features = null)
        {
            uint nextLevel = Level + 1u;
            return Class.GetExpFactor(features) * (nextLevel * nextLevel + nextLevel) / 2;
        }

        /// <summary>
        /// Adds the given amount of experience points to the
        /// party member. Returns true if the party members
        /// gained at least one level up.
        /// </summary>
        public bool AddExperiencePoints(uint amount, Func<int, int, int> random, Features? features = null)
        {
            ExperiencePoints += amount;

            if (Level == 50)
                return false;

            uint nextLevelExperiencePoints = GetNextLevelExperiencePoints(features);

            if (ExperiencePoints < nextLevelExperiencePoints)
                return false;

            do
            {
                ++Level;
                nextLevelExperiencePoints = GetNextLevelExperiencePoints(features);
                AddLevelUpEffects(random);
            }
            while (ExperiencePoints >= nextLevelExperiencePoints && Level < 50);

            return true;
        }

        public void AddLevelUpEffects(Func<int, int, int> random)
        {
            var intelligence = Attributes[Attribute.Intelligence].TotalCurrentValue;
            bool magicClass = Class.IsMagic();
            uint lpAdd = HitPointsPerLevel * (uint)random(50, 100) / 100;
            uint spAdd = magicClass ? SpellPointsPerLevel * (uint)random(50, 100) / 100 + intelligence / 25 : 0;
            uint slpAdd = magicClass ? SpellLearningPointsPerLevel * (uint)random(50, 100) / 100 + intelligence / 25 : 0;
            uint tpAdd = TrainingPointsPerLevel * (uint)random(50, 100) / 100;
            // In Ambermoon Advanced the level can decrease through exp exchanging.
            // SLP and TP won't be removed (as you might have spent parts of it).
            // But to avoid exploits, the max level a character ever reached is tracked.
            // And only if the character exceeds this, it will get SLP and TP.
            bool addSLPAndTP = MaxReachedLevel < Level;

            HitPoints.MaxValue += lpAdd;
            HitPoints.CurrentValue += lpAdd;
            if (magicClass && addSLPAndTP)
            {
                SpellPoints.MaxValue += spAdd;
                SpellPoints.CurrentValue += spAdd;
                SpellLearningPoints = (ushort)Math.Min(ushort.MaxValue, SpellLearningPoints + slpAdd);
            }
            if (addSLPAndTP)
                TrainingPoints = (ushort)Math.Min(ushort.MaxValue, TrainingPoints + tpAdd);
            AttacksPerRound = (byte)(AttacksPerRoundIncreaseLevels == 0 ? 1 : Util.Limit(AttacksPerRound, Level / AttacksPerRoundIncreaseLevels, 255));
            MaxReachedLevel = Math.Max(MaxReachedLevel, Level); // Update max reached level
        }
    }
}
