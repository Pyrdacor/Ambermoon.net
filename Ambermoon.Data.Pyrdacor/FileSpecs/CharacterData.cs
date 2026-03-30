using Ambermoon.Data.Enumerations;
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
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Character? character = null;

    public Character Character => character!;

    public CharacterData()
    {

    }

    public CharacterData(Character character)
    {
        this.character = character;
    }

    public void Read(IDataReader dataReader, uint index, GameData gameData, byte version)
    {
        switch (dataReader.PeekByte())
        {
            case 0: // party member
                ReadPartyMember(dataReader, index, gameData, version);
                break;
            case 1: // NPC
                ReadNPC(dataReader, index, gameData, version);
                break;
            case 2: // monster
                ReadMonster(dataReader, index, gameData, version);
                break;
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
            // TODO: There were some changes to some properties. Align with the legacy writer!

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
            dataWriter.WriteEnum8(partyMember.SpokenExtendedLanguages);
            dataWriter.Write((byte)(partyMember.MaxReachedLevel));
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
            // TODO: There were some changes to some properties. Align with the legacy writer!

            dataWriter.WriteEnum8(CharacterType.NPC);
            dataWriter.WriteEnum8(npc.Gender);
            dataWriter.WriteEnum8(npc.Race);
            dataWriter.WriteEnum8(npc.Class);
            dataWriter.Write((byte)npc.Level);
            dataWriter.WriteEnum8(npc.SpokenLanguages);
            dataWriter.Write((byte)npc.PortraitIndex);
            dataWriter.Write((byte)npc.JoinPercentage);
            dataWriter.WriteEnum8(npc.SpokenExtendedLanguages);
            dataWriter.Write((ushort)npc.Attributes[Attribute.Age].CurrentValue);
            dataWriter.Write((ushort)npc.LookAtCharTextIndex);
            dataWriter.Write(npc.Name);

            // Events
            EventData.WriteEvents(dataWriter, npc.Events, npc.EventList);
        }
        else if (character is Monster monster)
        {
            // TODO: There were some changes to some properties. Align with the legacy writer!

            dataWriter.WriteEnum8(CharacterType.Monster);
            dataWriter.WriteEnum8(monster.Gender);
            dataWriter.WriteEnum8(monster.Race);
            dataWriter.WriteEnum8(monster.Class);
            dataWriter.WriteEnum8(monster.SpellMastery);
            dataWriter.Write((byte)monster.Level);
            dataWriter.Write((byte)monster.NumberOfOccupiedHands);
            dataWriter.Write((byte)monster.NumberOfOccupiedFingers);
            dataWriter.WriteEnum8(monster.AdvancedMonsterFlags);
            dataWriter.WriteEnum8(monster.CombatGraphicIndex);
            dataWriter.Write((byte)monster.SpellChancePercentage);
            dataWriter.Write((byte)monster.MagicHitBonus);
            dataWriter.Write((byte)monster.Morale);
            dataWriter.WriteEnum8(monster.SpellTypeImmunity);
            dataWriter.Write((byte)monster.AttacksPerRound);
            dataWriter.WriteEnum8(monster.BattleFlags);
            dataWriter.WriteEnum8(monster.Element);
            dataWriter.Write((ushort)monster.Gold);
            dataWriter.Write((ushort)monster.Food);
            dataWriter.WriteEnum16(monster.Conditions);
            dataWriter.Write((ushort)monster.DefeatExperience);
            dataWriter.Write((ushort)monster.BattleRoundSpellPointUsage);
            foreach (var attribute in monster.Attributes) // Note: this includes Age and the 10th unused attribute
            {
                dataWriter.Write((ushort)attribute.CurrentValue);
                dataWriter.Write((ushort)attribute.MaxValue);
                dataWriter.WriteShort((short)attribute.BonusValue);
                dataWriter.Write((ushort)attribute.StoredValue);
            }
            foreach (var skill in monster.Skills)
            {
                dataWriter.Write((ushort)skill.CurrentValue);
                dataWriter.Write((ushort)skill.MaxValue);
                dataWriter.WriteShort((short)skill.BonusValue);
                dataWriter.Write((ushort)skill.StoredValue);
            }
            dataWriter.Write((ushort)monster.HitPoints.CurrentValue);
            dataWriter.Write((ushort)monster.HitPoints.MaxValue);
            dataWriter.WriteShort((short)monster.HitPoints.BonusValue);
            dataWriter.Write((ushort)monster.SpellPoints.CurrentValue);
            dataWriter.Write((ushort)monster.SpellPoints.MaxValue);
            dataWriter.WriteShort((short)monster.SpellPoints.BonusValue);
            dataWriter.WriteShort(monster.BaseDefense);
            dataWriter.WriteShort(monster.BonusDefense);
            dataWriter.WriteShort(monster.BaseAttackDamage);
            dataWriter.WriteShort(monster.BonusAttackDamage);
            dataWriter.WriteShort(monster.MagicAttack);
            dataWriter.WriteShort(monster.MagicDefense);
            dataWriter.Write(monster.LearnedHealingSpells);
            dataWriter.Write(monster.LearnedAlchemisticSpells);
            dataWriter.Write(monster.LearnedMysticSpells);
            dataWriter.Write(monster.LearnedDestructionSpells);
            dataWriter.Write(monster.LearnedSpellsType5);
            dataWriter.Write(monster.LearnedSpellsType6);
            dataWriter.Write(monster.LearnedSpellsType7);
            dataWriter.Write(monster.Name);

            // Equipment
            foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
            {
                if (equipmentSlot != EquipmentSlot.None)
                    ItemSlotWriter.WriteItemSlot(monster.Equipment.Slots[equipmentSlot], dataWriter);
            }

            // Inventory
            for (int i = 0; i < Inventory.Width * Inventory.Height; ++i)
                ItemSlotWriter.WriteItemSlot(monster.Inventory.Slots[i], dataWriter);

            // Monster data
            foreach (var animation in monster.Animations)
                dataWriter.Write(animation.FrameIndices);

            foreach (var animation in monster.Animations)
                dataWriter.Write((byte)animation.UsedAmount);

            dataWriter.Write(monster.MonsterPalette);
            dataWriter.Write(monster.AlternateAnimationBits);
            dataWriter.Write(monster.PaddingByte);
            dataWriter.Write((ushort)monster.FrameWidth);
            dataWriter.Write((ushort)monster.FrameHeight);
            dataWriter.Write((ushort)monster.MappedFrameWidth);
            dataWriter.Write((ushort)monster.MappedFrameHeight);
        }
        else
        {
            throw new AmbermoonException(ExceptionScope.Application, "Invalid character type.");
        }
    }

    private void ReadNPC(IDataReader dataReader, uint index, GameData gameData, byte _)
    {
        var npc = new NPC()
        {
            Index = index
        };

        dataReader.Position++; // skip character type

        // TODO: There were some changes to some properties. Align with the legacy reader!

        npc.Gender = dataReader.ReadEnum8<Gender>();
        npc.Race = dataReader.ReadEnum8<Race>();
        npc.Class = dataReader.ReadEnum8<Class>();
        npc.Level = dataReader.ReadByte();
        npc.SpokenLanguages = dataReader.ReadEnum8<Language>();
        npc.PortraitIndex = dataReader.ReadByte();
        npc.JoinPercentage = dataReader.ReadByte();
        npc.SpokenExtendedLanguages = dataReader.ReadEnum8<ExtendedLanguage>();
        npc.Attributes[Attribute.Age].CurrentValue = dataReader.ReadWord();
        npc.LookAtCharTextIndex = dataReader.ReadWord();
        npc.Name = dataReader.ReadString();

        // Events
        EventData.ReadEvents(dataReader, npc.Events, npc.EventList);

        // Texts
        npc.Texts = gameData.NPCTexts.TryGetValue(index, out var texts) ? texts.ToList() : [];

        character = npc;
    }

    private void ReadPartyMember(IDataReader dataReader, uint index, GameData gameData, byte _)
    {
        var partyMember = new PartyMember()
        {
            Index = index
        };

        dataReader.Position++; // skip character type

        // TODO: There were some changes to some properties. Align with the legacy reader!

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
        partyMember.SpokenExtendedLanguages = dataReader.ReadEnum8<ExtendedLanguage>();
        partyMember.MaxReachedLevel = dataReader.ReadByte();
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

    private void ReadMonster(IDataReader dataReader, uint index, GameData gameData, byte _)
    {
        var monster = new Monster()
        {
            Index = index
        };

        dataReader.Position++; // skip character type

        // TODO: There were some changes to some properties. Align with the legacy reader!

        monster.Gender = dataReader.ReadEnum8<Gender>();
        monster.Race = dataReader.ReadEnum8<Race>();
        monster.Class = dataReader.ReadEnum8<Class>();
        monster.SpellMastery = dataReader.ReadEnum8<SpellTypeMastery>();
        monster.Level = dataReader.ReadByte();
        monster.NumberOfOccupiedHands = dataReader.ReadByte();
        monster.NumberOfOccupiedFingers = dataReader.ReadByte();
        monster.AdvancedMonsterFlags = dataReader.ReadEnum8<AdvancedMonsterFlags>();
        monster.CombatGraphicIndex = dataReader.ReadEnum8<MonsterGraphicIndex>();
        monster.SpellChancePercentage = dataReader.ReadByte();
        monster.MagicHitBonus = dataReader.ReadByte();
        monster.Morale = dataReader.ReadByte();
        monster.SpellTypeImmunity = dataReader.ReadEnum8<SpellTypeImmunity>();
        monster.AttacksPerRound = dataReader.ReadByte();
        monster.BattleFlags = dataReader.ReadEnum8<BattleFlags>();
        monster.Element = dataReader.ReadEnum8<CharacterElement>();
        monster.Gold = dataReader.ReadWord();
        monster.Food = dataReader.ReadWord();
        monster.Conditions = dataReader.ReadEnum16<Condition>();
        monster.DefeatExperience = dataReader.ReadWord();
        monster.BattleRoundSpellPointUsage = dataReader.ReadWord();

        foreach (var attribute in monster.Attributes) // Note: this includes Age and the 10th unused attribute
        {
            attribute.CurrentValue = dataReader.ReadWord();
            attribute.MaxValue = dataReader.ReadWord();
            attribute.BonusValue = dataReader.ReadShort();
            attribute.StoredValue = dataReader.ReadWord();
        }

        foreach (var skill in monster.Skills)
        {
            skill.CurrentValue = dataReader.ReadWord();
            skill.MaxValue = dataReader.ReadWord();
            skill.BonusValue = dataReader.ReadShort();
            skill.StoredValue = dataReader.ReadWord();
        }

        monster.HitPoints.CurrentValue = dataReader.ReadWord();
        monster.HitPoints.MaxValue = dataReader.ReadWord();
        monster.HitPoints.BonusValue = dataReader.ReadShort();
        monster.SpellPoints.CurrentValue = dataReader.ReadWord();
        monster.SpellPoints.MaxValue = dataReader.ReadWord();
        monster.SpellPoints.BonusValue = dataReader.ReadShort();
        monster.BaseDefense = dataReader.ReadShort();
        monster.BonusDefense = dataReader.ReadShort();
        monster.BaseAttackDamage = dataReader.ReadShort();
        monster.BonusAttackDamage = dataReader.ReadShort();
        monster.MagicAttack = dataReader.ReadShort();
        monster.MagicDefense = dataReader.ReadShort();
        monster.LearnedHealingSpells = dataReader.ReadDword();
        monster.LearnedAlchemisticSpells = dataReader.ReadDword();
        monster.LearnedMysticSpells = dataReader.ReadDword();
        monster.LearnedDestructionSpells = dataReader.ReadDword();
        monster.LearnedSpellsType5 = dataReader.ReadDword();
        monster.LearnedSpellsType6 = dataReader.ReadDword();
        monster.LearnedSpellsType7 = dataReader.ReadDword();
        monster.Name = dataReader.ReadString();

        // Equipment
        foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
        {
            if (equipmentSlot != EquipmentSlot.None)
                ItemSlotReader.ReadItemSlot(monster.Equipment.Slots[equipmentSlot], dataReader);
        }

        // Inventory
        for (int i = 0; i < Inventory.Width * Inventory.Height; ++i)
            ItemSlotReader.ReadItemSlot(monster.Inventory.Slots[i], dataReader);

        // Monster data
        for (int i = 0; i < 8; ++i)
        {
            monster.Animations[i] = new Monster.Animation
            {
                FrameIndices = dataReader.ReadBytes(32)
            };
        }

        foreach (var animation in monster.Animations)
            animation.UsedAmount = dataReader.ReadByte();

        monster.AtariPalette = new byte[16];
        monster.MonsterPalette = dataReader.ReadBytes(32);        
        monster.AlternateAnimationBits = dataReader.ReadByte();
        monster.PaddingByte = dataReader.ReadByte();
        monster.FrameWidth = dataReader.ReadWord();
        monster.FrameHeight = dataReader.ReadWord();
        monster.MappedFrameWidth = dataReader.ReadWord();
        monster.MappedFrameHeight = dataReader.ReadWord();

        character = monster;
    }
}
