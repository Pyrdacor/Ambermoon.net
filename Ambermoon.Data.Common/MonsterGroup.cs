using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class MonsterGroup
    {
        public Monster[,] Monsters { get; } = new Monster[6, 3];

        public static MonsterGroup Load(ICharacterManager characterManager, IMonsterGroupReader monsterGroupReader, IDataReader dataReader)
        {
            var monsterGroup = new MonsterGroup();

            monsterGroupReader.ReadMonsterGroup(characterManager, monsterGroup, dataReader);

            return monsterGroup;
        }
    }
}
