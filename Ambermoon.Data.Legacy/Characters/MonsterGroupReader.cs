namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterGroupReader : IMonsterGroupReader
    {
        public void ReadMonsterGroup(ICharacterManager characterManager, MonsterGroup monsterGroup, IDataReader dataReader)
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 6; ++c)
                {
                    uint monsterIndex = dataReader.ReadWord();

                    monsterGroup.Monsters[c, r] = monsterIndex == 0 ? null :
                        characterManager.GetMonster(monsterIndex);
                }
            }
        }
    }
}
