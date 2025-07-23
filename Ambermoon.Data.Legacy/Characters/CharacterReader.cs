using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;

namespace Ambermoon.Data.Legacy.Characters
{
    public abstract class CharacterReader
    {
        internal void ReadCharacter(Character character, IDataReader dataReader)
        {
            dataReader.Position = 0;

            if (dataReader.ReadByte() != (byte)character.Type)
                throw new Exception("Wrong character type.");

            character.Gender = (Gender)dataReader.ReadByte();
            character.Race = (Race)dataReader.ReadByte();
            character.Class = (Class)dataReader.ReadByte();
            character.SpellMastery = (SpellTypeMastery)dataReader.ReadByte();
            character.Level = dataReader.ReadByte();
            character.NumberOfOccupiedHands = dataReader.ReadByte();
            character.NumberOfOccupiedFingers = dataReader.ReadByte();
            character.SpokenLanguages = (Language)dataReader.ReadByte();
            character.InventoryInaccessible = dataReader.ReadByte() != 0;
            character.PortraitIndex = dataReader.ReadByte();
            if (character is Monster monster)
                monster.AdvancedMonsterFlags = (AdvancedMonsterFlags)dataReader.ReadByte();
            else
                character.JoinPercentage = dataReader.ReadByte();
            ProcessIfMonster(dataReader, character, (Monster monster, byte value) => monster.CombatGraphicIndex = (MonsterGraphicIndex)value);
            if (character is Monster)
                character.SpellChancePercentage = dataReader.ReadByte();
            else
                character.SpokenExtendedLanguages = (ExtendedLanguage)dataReader.ReadByte();
            character.MagicHitBonus = dataReader.ReadByte();
            ProcessIfMonsterOrPartyMember(dataReader, character, (Monster monster, byte value) => monster.Morale = value,
                (PartyMember partyMember, byte value) => partyMember.MaxReachedLevel = value);
            character.SpellTypeImmunity = (SpellTypeImmunity)dataReader.ReadByte();
            character.AttacksPerRound = dataReader.ReadByte();
            character.BattleFlags = (BattleFlags)dataReader.ReadByte();
            character.Element = (CharacterElement)dataReader.ReadByte();
            character.SpellLearningPoints = dataReader.ReadWord();
            character.TrainingPoints = dataReader.ReadWord();
            character.Gold = dataReader.ReadWord();
            character.Food = dataReader.ReadWord();
            character.CharacterBitIndex = dataReader.ReadWord();
            character.Conditions = (Condition)dataReader.ReadWord();
            ProcessIfMonster(dataReader, character, (Monster monster, ushort value) => monster.DefeatExperience = value);
            character.BattleRoundSpellPointUsage = dataReader.ReadWord(); // Unknown
            // mark of return location is stored here: word x, word y, word mapIndex
            ProcessIfPartyMember(dataReader, character, (PartyMember member, ushort value) => member.MarkOfReturnX = value);
            ProcessIfPartyMember(dataReader, character, (PartyMember member, ushort value) => member.MarkOfReturnY = value);
            ProcessIfPartyMember(dataReader, character, (PartyMember member, ushort value) => member.MarkOfReturnMapIndex = value);
            foreach (var attribute in character.Attributes) // Note: this includes Age and the 10th unused attribute
            {
                attribute.CurrentValue = dataReader.ReadWord();
                attribute.MaxValue = dataReader.ReadWord();
                attribute.BonusValue = (short)dataReader.ReadWord();
                attribute.StoredValue = dataReader.ReadWord();
            }
            foreach (var skill in character.Skills)
            {
                skill.CurrentValue = dataReader.ReadWord();
                skill.MaxValue = dataReader.ReadWord();
                skill.BonusValue = (short)dataReader.ReadWord();
                skill.StoredValue = dataReader.ReadWord();
            }
            character.HitPoints.CurrentValue = dataReader.ReadWord();
            character.HitPoints.MaxValue = dataReader.ReadWord();
            character.HitPoints.BonusValue = (short)dataReader.ReadWord();
            character.SpellPoints.CurrentValue = dataReader.ReadWord();
            character.SpellPoints.MaxValue = dataReader.ReadWord();
            character.SpellPoints.BonusValue = (short)dataReader.ReadWord();
            character.BaseDefense = (short)dataReader.ReadWord();
            character.BonusDefense = (short)dataReader.ReadWord();
            character.BaseAttackDamage = (short)dataReader.ReadWord();
            character.BonusAttackDamage = (short)dataReader.ReadWord();
            character.MagicAttack = (short)dataReader.ReadWord();
            character.MagicDefense = (short)dataReader.ReadWord();
            character.AttacksPerRoundIncreaseLevels = dataReader.ReadWord();
            character.HitPointsPerLevel = dataReader.ReadWord();
            character.SpellPointsPerLevel = dataReader.ReadWord();
            character.SpellLearningPointsPerLevel = dataReader.ReadWord();
            character.TrainingPointsPerLevel = dataReader.ReadWord();
            character.LookAtCharTextIndex = dataReader.ReadWord();
            character.ExperiencePoints = dataReader.ReadDword();
            character.LearnedHealingSpells = dataReader.ReadDword();
            character.LearnedAlchemisticSpells = dataReader.ReadDword();
            character.LearnedMysticSpells = dataReader.ReadDword();
            character.LearnedDestructionSpells = dataReader.ReadDword();
            character.LearnedSpellsType5 = dataReader.ReadDword();
            character.LearnedSpellsType6 = dataReader.ReadDword();
            character.LearnedSpellsType7 = dataReader.ReadDword();
            character.TotalWeight = dataReader.ReadDword();
            character.Name = dataReader.ReadString(16);

            int terminatingNullIndex = character.Name.IndexOf('\0');

            if (terminatingNullIndex != 0)
                character.Name = character.Name.Substring(0, terminatingNullIndex).TrimEnd();
            else
                character.Name = character.Name.TrimEnd();

            if (character is not Monster && character.LookAtCharTextIndex == 0xffff)
                character.LookAtCharTextIndex = 0; // fallback to text index 0, as there are some flawed characters

            if (character.Type != CharacterType.NPC)
            {
                // Equipment
                foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
                {
                    if (equipmentSlot != EquipmentSlot.None)
                        ItemSlotReader.ReadItemSlot(character.Equipment.Slots[equipmentSlot], dataReader);
                }

                // Inventory
                for (int i = 0; i < Inventory.Width * Inventory.Height; ++i)
                    ItemSlotReader.ReadItemSlot(character.Inventory.Slots[i], dataReader);
            }
        }

        void ProcessIfMonster(IDataReader reader, Character character, Action<Monster, byte> processor)
        {
            if (character is Monster monster)
                processor(monster, reader.ReadByte());
            else
                reader.Position += 1;
        }

        void ProcessIfMonster(IDataReader reader, Character character, Action<Monster, ushort> processor)
        {
            if (character is Monster monster)
                processor(monster, reader.ReadWord());
            else
                reader.Position += 2;
        }

        void ProcessIfPartyMember(IDataReader reader, Character character, Action<PartyMember, ushort> processor)
        {
            if (character is PartyMember partyMember)
                processor(partyMember, reader.ReadWord());
            else
                reader.Position += 2;
        }

        void ProcessIfMonsterOrPartyMember(IDataReader reader, Character character, Action<Monster, byte> monsterProcessor,
            Action<PartyMember, byte> partyMemberProcessor)
        {
            if (character is Monster monster)
                monsterProcessor(monster, reader.ReadByte());
            else if (character is PartyMember partyMember)
                partyMemberProcessor(partyMember, reader.ReadByte());
            else
                reader.Position += 1;
        }
    }
}
