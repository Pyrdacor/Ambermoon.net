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
            dataReader.ReadByte(); // TODO: spells
            character.Level = dataReader.ReadByte();
            dataReader.Position += 2; // Unknown
            dataReader.ReadByte(); // TODO: spoken languages
            character.PortraitIndex = dataReader.ReadWord();
            dataReader.Position += 263; // TODO: skip everything for now till the name
            character.Name = dataReader.ReadString(16).Replace('\0', ' ').TrimEnd();
            // TODO: ignore the rest for now
        }
    }
}
