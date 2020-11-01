using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterWriter : CharacterWriter
    {
        public void WriteMonster(Monster monster, IDataWriter dataWriter)
        {
            WriteCharacter(monster, dataWriter);

            // TODO: monsters have some additional data (maybe fight animations?)
            dataWriter.Write(monster.UnknownAdditionalBytes);
        }
    }
}
