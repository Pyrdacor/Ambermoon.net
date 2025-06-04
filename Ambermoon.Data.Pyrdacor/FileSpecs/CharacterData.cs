using System.Diagnostics;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class CharacterData : IFileSpec<CharacterData>, IFileSpec
{
    public static string Magic => "CHR";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Character? character = null;

    public Character Character => character!;

    public CharacterData()
    {

    }

    public CharacterData(Character character)
    {
        this.character = character;
    }

    public void Read(IDataReader dataReader, uint index, GameData gameData)
    {
        switch (dataReader.PeekByte())
        {
            case 0: // party member
                ReadPartyMember(dataReader, index, gameData);
                break;
            case 1: // NPC
                ReadNPC(dataReader, index, gameData);
                break;
            case 2: // monster
                // TODO
                throw new NotImplementedException();
            default:
                throw new AmbermoonException(ExceptionScope.Data, "Invalid character data.");
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        if (character == null)
            throw new AmbermoonException(ExceptionScope.Application, "Character data was null when trying to write it.");

        if (character is PartyMember partyMember)
        {
            dataWriter.WriteEnum8(CharacterType.PartyMember);
            dataWriter.WriteEnum8(partyMember.Gender);
            dataWriter.WriteEnum8(partyMember.Race);
            dataWriter.WriteEnum8(partyMember.Class);
            dataWriter.WriteEnum8(partyMember.SpellMastery);
            dataWriter.Write((byte)partyMember.Level);
            dataWriter.Write((byte)partyMember.NumberOfOccupiedHands);
            dataWriter.Write((byte)partyMember.NumberOfOccupiedFingers);
            dataWriter.WriteEnum8(partyMember.SpokenLanguages);
            dataWriter.Write((byte)(partyMember.InventoryInaccessible ? 0xff : 0));
            dataWriter.Write((byte)partyMember.PortraitIndex);
            dataWriter.Write((byte)partyMember.JoinPercentage);
            dataWriter.WriteEnum8(partyMember.SpellTypeImmunity);
            dataWriter.Write((byte)partyMember.AttacksPerRound);
            dataWriter.WriteEnum8(partyMember.BattleFlags);
            dataWriter.WriteEnum8(partyMember.Element);
            dataWriter.Write((ushort)partyMember.SpellLearningPoints);
            dataWriter.Write((ushort)partyMember.TrainingPoints);
            dataWriter.Write((ushort)partyMember.Gold);
            dataWriter.Write((ushort)partyMember.Food);
            dataWriter.Write((ushort)partyMember.CharacterBitIndex);
            dataWriter.WriteEnum16(partyMember.Conditions);
            dataWriter.Write((ushort)partyMember.MarkOfReturnX);
            dataWriter.Write((ushort)partyMember.MarkOfReturnY);
            dataWriter.Write((ushort)partyMember.MarkOfReturnMapIndex);
            foreach (var attribute in partyMember.Attributes) // Note: this includes Age and the 10th unused attribute
            {
                dataWriter.Write((ushort)attribute.CurrentValue);
                dataWriter.Write((ushort)attribute.MaxValue);
                dataWriter.WriteShort((short)attribute.BonusValue);
                dataWriter.Write((ushort)attribute.StoredValue);
            }
            foreach (var skill in partyMember.Skills)
            {
                dataWriter.Write((ushort)skill.CurrentValue);
                dataWriter.Write((ushort)skill.MaxValue);
                dataWriter.WriteShort((short)skill.BonusValue);
                dataWriter.Write((ushort)skill.StoredValue);
            }
            dataWriter.Write((ushort)partyMember.HitPoints.CurrentValue);
            dataWriter.Write((ushort)partyMember.HitPoints.MaxValue);
            dataWriter.WriteShort((short)partyMember.HitPoints.BonusValue);
            dataWriter.Write((ushort)partyMember.SpellPoints.CurrentValue);
            dataWriter.Write((ushort)partyMember.SpellPoints.MaxValue);
            dataWriter.WriteShort((short)partyMember.SpellPoints.BonusValue);
            dataWriter.WriteShort(partyMember.BaseDefense);
            dataWriter.WriteShort(partyMember.BonusDefense);
            dataWriter.WriteShort(partyMember.BaseAttackDamage);
            dataWriter.WriteShort(partyMember.BonusAttackDamage);
            dataWriter.WriteShort(partyMember.MagicAttack);
            dataWriter.WriteShort(partyMember.MagicDefense);
            dataWriter.Write((ushort)partyMember.AttacksPerRoundIncreaseLevels);
            dataWriter.Write((ushort)partyMember.HitPointsPerLevel);
            dataWriter.Write((ushort)partyMember.SpellPointsPerLevel);
            dataWriter.Write((ushort)partyMember.SpellLearningPointsPerLevel);
            dataWriter.Write((ushort)partyMember.TrainingPointsPerLevel);
            dataWriter.Write((ushort)partyMember.LookAtCharTextIndex);
            dataWriter.Write(partyMember.ExperiencePoints);
            dataWriter.Write(partyMember.LearnedHealingSpells);
            dataWriter.Write(partyMember.LearnedAlchemisticSpells);
            dataWriter.Write(partyMember.LearnedMysticSpells);
            dataWriter.Write(partyMember.LearnedDestructionSpells);
            dataWriter.Write(partyMember.LearnedSpellsType5);
            dataWriter.Write(partyMember.LearnedSpellsType6);
            dataWriter.Write(partyMember.LearnedSpellsType7);
            dataWriter.Write(partyMember.TotalWeight);
            dataWriter.Write(partyMember.Name);

            // Equipment
            foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
            {
                if (equipmentSlot != EquipmentSlot.None)
                    ItemSlotWriter.WriteItemSlot(partyMember.Equipment.Slots[equipmentSlot], dataWriter);
            }

            // Inventory
            for (int i = 0; i < Inventory.Width * Inventory.Height; ++i)
                ItemSlotWriter.WriteItemSlot(partyMember.Inventory.Slots[i], dataWriter);

            // Events
            EventData.WriteEvents(dataWriter, partyMember.Events, partyMember.EventList);
        }
        else if (character is NPC npc)
        {
            dataWriter.WriteEnum8(CharacterType.NPC);
            dataWriter.WriteEnum8(npc.Gender);
            dataWriter.WriteEnum8(npc.Race);
            dataWriter.WriteEnum8(npc.Class);
            dataWriter.Write((byte)npc.Level);
            dataWriter.WriteEnum8(npc.SpokenLanguages);
            dataWriter.Write((byte)npc.PortraitIndex);
            dataWriter.Write((ushort)npc.Attributes[Attribute.Age].CurrentValue);
            dataWriter.Write((ushort)npc.LookAtCharTextIndex);
            dataWriter.Write(npc.Name);

            // Events
            EventData.WriteEvents(dataWriter, npc.Events, npc.EventList);
        }
        else if (character is Monster monster)
        {
            // TODO
            throw new NotImplementedException();
        }
        else
        {
            throw new AmbermoonException(ExceptionScope.Application, "Invalid character type.");
        }
    }

    private void ReadNPC(IDataReader dataReader, uint index, GameData gameData)
    {
        var npc = new NPC()
        {
            Index = index
        };

        dataReader.Position++; // skip character type

        npc.Gender = dataReader.ReadEnum8<Gender>();
        npc.Race = dataReader.ReadEnum8<Race>();
        npc.Class = dataReader.ReadEnum8<Class>();
        npc.Level = dataReader.ReadByte();
        npc.SpokenLanguages = dataReader.ReadEnum8<Language>();
        npc.PortraitIndex = dataReader.ReadByte();
        npc.Attributes[Attribute.Age].CurrentValue = dataReader.ReadWord();
        npc.LookAtCharTextIndex = dataReader.ReadWord();
        npc.Name = dataReader.ReadString();

        // Events
        EventData.ReadEvents(dataReader, npc.Events, npc.EventList);

        // Texts
        npc.Texts = gameData.NPCTexts.TryGetValue(index, out var texts) ? texts.ToList() : [];

        character = npc;
    }

    private void ReadPartyMember(IDataReader dataReader, uint index, GameData gameData)
    {
        var partyMember = new PartyMember()
        {
            Index = index
        };

        dataReader.Position++; // skip character type

        partyMember.Gender = dataReader.ReadEnum8<Gender>();
        partyMember.Race = dataReader.ReadEnum8<Race>();
        partyMember.Class = dataReader.ReadEnum8<Class>();
        partyMember.SpellMastery = dataReader.ReadEnum8<SpellTypeMastery>();
        partyMember.Level = dataReader.ReadByte();
        partyMember.NumberOfOccupiedHands = dataReader.ReadByte();
        partyMember.NumberOfOccupiedFingers = dataReader.ReadByte();
        partyMember.SpokenLanguages = dataReader.ReadEnum8<Language>();
        partyMember.InventoryInaccessible = dataReader.ReadByte() != 0;
        partyMember.PortraitIndex = dataReader.ReadByte();
        partyMember.JoinPercentage = dataReader.ReadByte();
        partyMember.SpellTypeImmunity = dataReader.ReadEnum8<SpellTypeImmunity>();
        partyMember.AttacksPerRound = dataReader.ReadByte();
        partyMember.BattleFlags = dataReader.ReadEnum8<BattleFlags>();
        partyMember.Element = dataReader.ReadEnum8<CharacterElement>();
        partyMember.SpellLearningPoints = dataReader.ReadWord();
        partyMember.TrainingPoints = dataReader.ReadWord();
        partyMember.Gold = dataReader.ReadWord();
        partyMember.Food = dataReader.ReadWord();
        partyMember.CharacterBitIndex = dataReader.ReadWord();
        partyMember.Conditions = dataReader.ReadEnum16<Condition>();

        partyMember.MarkOfReturnX = dataReader.ReadWord();
        partyMember.MarkOfReturnY = dataReader.ReadWord();
        partyMember.MarkOfReturnMapIndex = dataReader.ReadWord();

        foreach (var attribute in partyMember.Attributes) // Note: this includes Age and the 10th unused attribute
        {
            attribute.CurrentValue = dataReader.ReadWord();
            attribute.MaxValue = dataReader.ReadWord();
            attribute.BonusValue = dataReader.ReadShort();
            attribute.StoredValue = dataReader.ReadWord();
        }

        foreach (var skill in partyMember.Skills)
        {
            skill.CurrentValue = dataReader.ReadWord();
            skill.MaxValue = dataReader.ReadWord();
            skill.BonusValue = dataReader.ReadShort();
            skill.StoredValue = dataReader.ReadWord();
        }

        partyMember.HitPoints.CurrentValue = dataReader.ReadWord();
        partyMember.HitPoints.MaxValue = dataReader.ReadWord();
        partyMember.HitPoints.BonusValue = dataReader.ReadShort();
        partyMember.SpellPoints.CurrentValue = dataReader.ReadWord();
        partyMember.SpellPoints.MaxValue = dataReader.ReadWord();
        partyMember.SpellPoints.BonusValue = dataReader.ReadShort();
        partyMember.BaseDefense = dataReader.ReadShort();
        partyMember.BonusDefense = dataReader.ReadShort();
        partyMember.BaseAttackDamage = dataReader.ReadShort();
        partyMember.BonusAttackDamage = dataReader.ReadShort();
        partyMember.MagicAttack = dataReader.ReadShort();
        partyMember.MagicDefense = dataReader.ReadShort();
        partyMember.AttacksPerRoundIncreaseLevels = dataReader.ReadWord();
        partyMember.HitPointsPerLevel = dataReader.ReadWord();
        partyMember.SpellPointsPerLevel = dataReader.ReadWord();
        partyMember.SpellLearningPointsPerLevel = dataReader.ReadWord();
        partyMember.TrainingPointsPerLevel = dataReader.ReadWord();
        partyMember.LookAtCharTextIndex = dataReader.ReadWord();
        partyMember.ExperiencePoints = dataReader.ReadDword();
        partyMember.LearnedHealingSpells = dataReader.ReadDword();
        partyMember.LearnedAlchemisticSpells = dataReader.ReadDword();
        partyMember.LearnedMysticSpells = dataReader.ReadDword();
        partyMember.LearnedDestructionSpells = dataReader.ReadDword();
        partyMember.LearnedSpellsType5 = dataReader.ReadDword();
        partyMember.LearnedSpellsType6 = dataReader.ReadDword();
        partyMember.LearnedSpellsType7 = dataReader.ReadDword();
        partyMember.TotalWeight = dataReader.ReadDword();
        partyMember.Name = dataReader.ReadString();

        // Equipment
        foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
        {
            if (equipmentSlot != EquipmentSlot.None)
                ItemSlotReader.ReadItemSlot(partyMember.Equipment.Slots[equipmentSlot], dataReader);
        }

        // Inventory
        for (int i = 0; i < Inventory.Width * Inventory.Height; ++i)
            ItemSlotReader.ReadItemSlot(partyMember.Inventory.Slots[i], dataReader);

        // Events
        EventData.ReadEvents(dataReader, partyMember.Events, partyMember.EventList);

        // Texts
        partyMember.Texts = gameData.PartyTexts.TryGetValue(index, out var texts) ? texts.ToList() : [];

        character = partyMember;
    }
}
