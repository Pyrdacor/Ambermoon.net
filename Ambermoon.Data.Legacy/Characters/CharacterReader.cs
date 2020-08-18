using System;

namespace Ambermoon.Data.Legacy.Characters
{
    public abstract class CharacterReader
    {
        internal void ReadCharacter(Character character, IDataReader dataReader)
        {
            if (dataReader.ReadByte() != (byte)character.Type)
                throw new Exception("Wrong character type.");

            character.Gender = (Gender)dataReader.ReadByte();
            character.Race = (Race)dataReader.ReadByte();
            character.Class = (Class)dataReader.ReadByte();
            character.SpellMastery = (SpellTypeMastery)dataReader.ReadByte();
            character.Level = dataReader.ReadByte();
            dataReader.Position += 2; // Unknown
            character.SpokenLanguages = (Language)dataReader.ReadByte();
            character.PortraitIndex = dataReader.ReadWord();
            ProcessIfMonster(dataReader, character, (Monster monster, ushort value) => monster.Unknown1 = value);
            dataReader.Position += 2; // Unknown
            ProcessIfMonster(dataReader, character, (Monster monster, byte value) => monster.HitChance = value);
            dataReader.Position += 1; // Unknown
            character.AttacksPerRound = dataReader.ReadByte();
            ProcessIfMonster(dataReader, character, (Monster monster, byte value) => monster.MonsterFlags = (MonsterFlags)value);
            ProcessIfMonster(dataReader, character, (Monster monster, byte value) => monster.Element = (MonsterElement)value);
            character.SpellLearningPoints = dataReader.ReadWord();
            character.TrainingPoints = dataReader.ReadWord();
            character.Gold = dataReader.ReadWord();
            character.Food = dataReader.ReadWord();
            dataReader.Position += 2; // Unknown
            character.Ailments = (Ailment)dataReader.ReadWord();
            ProcessIfMonster(dataReader, character, (Monster monster, ushort value) => monster.DefeatExperience = value);
            dataReader.Position += 8; // Unknown
            foreach (var attribute in character.Attributes) // Note: this includes Age and the 10th unused attribute
            {
                attribute.CurrentValue = dataReader.ReadWord();
                attribute.MaxValue = dataReader.ReadWord();
                attribute.BonusValue = dataReader.ReadWord();
                attribute.Unknown = dataReader.ReadWord();
            }
            foreach (var ability in character.Abilities)
            {
                ability.CurrentValue = dataReader.ReadWord();
                ability.MaxValue = dataReader.ReadWord();
                ability.BonusValue = dataReader.ReadWord();
                ability.Unknown = dataReader.ReadWord();
            }
            character.HitPoints.CurrentValue = dataReader.ReadWord();
            character.HitPoints.MaxValue = dataReader.ReadWord();
            character.HitPoints.BonusValue = dataReader.ReadWord();
            character.SpellPoints.CurrentValue = dataReader.ReadWord();
            character.SpellPoints.MaxValue = dataReader.ReadWord();
            character.SpellPoints.BonusValue = dataReader.ReadWord();
            dataReader.Position += 2; // Unknown
            character.Defense = dataReader.ReadWord();
            dataReader.Position += 2; // Unknown
            character.Attack = dataReader.ReadWord();
            character.MagicAttack = dataReader.ReadWord();
            character.MagicDefense = dataReader.ReadWord();
            character.AttacksPerRoundPerLevel = dataReader.ReadWord();
            character.HitPointsPerLevel = dataReader.ReadWord();
            character.SpellPointsPerLevel = dataReader.ReadWord();
            character.SpellLearningPointsPerLevel = dataReader.ReadWord();
            character.TrainingPointsPerLevel = dataReader.ReadWord();
            dataReader.Position += 2; // Unknown
            character.ExperiencePoints = dataReader.ReadDword();
            character.LearnedHealingSpells = dataReader.ReadDword();
            character.LearnedAlchemisticSpells = dataReader.ReadDword();
            character.LearnedMysticSpells = dataReader.ReadDword();
            character.LearnedDestructionSpells = dataReader.ReadDword();
            dataReader.Position += 12; // Unknown
            character.TotalWeight = dataReader.ReadDword();
            character.Name = dataReader.ReadString(16).Replace('\0', ' ').TrimEnd();
            // TODO: ignore the rest for now
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
    }
}
