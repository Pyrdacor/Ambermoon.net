using Ambermoon.Data.Serialization;
using System.Collections.Generic;

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

            if (weaponSlot.Empty || weaponSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                return false;

            var weapon = itemManager.GetItem(weaponSlot.ItemIndex);

            if (weapon.Type == ItemType.LongRangeWeapon)
                return HasAmmunition(itemManager, weapon.UsedAmmunitionType);

            return true;
        }
    }
}
