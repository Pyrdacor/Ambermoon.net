namespace Ambermoon.Data
{
    public class Monster : Character
    {
        private Monster()
            : base(CharacterType.Monster)
        {

        }

        public static Monster Load(IMonsterReader monsterReader, IDataReader dataReader)
        {
            var monster = new Monster();

            monsterReader.ReadMonster(monster, dataReader);

            return monster;
        }
    }
}
