namespace Ambermoon.Data
{
    public interface IMonsterGroupReader
    {
        void ReadMonsterGroup(ICharacterManager characterManager, MonsterGroup monsterGroup, IDataReader dataReader);
    }
}
