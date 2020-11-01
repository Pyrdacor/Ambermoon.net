namespace Ambermoon.Data.Serialization
{
    public interface IMonsterReader
    {
        void ReadMonster(Monster monster, IDataReader dataReader);
    }
}
