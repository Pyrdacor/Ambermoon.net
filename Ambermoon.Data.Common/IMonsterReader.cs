namespace Ambermoon.Data
{
    public interface IMonsterReader
    {
        void ReadMonster(Monster monster, IDataReader dataReader);
    }
}
