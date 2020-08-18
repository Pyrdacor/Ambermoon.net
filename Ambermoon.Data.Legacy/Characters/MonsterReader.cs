namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterReader : CharacterReader, IMonsterReader
    {
        public void ReadMonster(Monster monster, IDataReader dataReader)
        {
            ReadCharacter(monster, dataReader);

            // TODO: monsters have some additional data (maybe fight animations?)
        }
    }
}
