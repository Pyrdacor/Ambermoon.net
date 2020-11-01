namespace Ambermoon.Data.Serialization
{
    public interface IMonsterGroupReader
    {
        void ReadMonsterGroup(ICharacterManager characterManager, MonsterGroup monsterGroup, IDataReader dataReader);
    }
}
