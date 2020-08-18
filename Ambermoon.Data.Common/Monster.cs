namespace Ambermoon.Data
{
    public class Monster : Character
    {
        public ushort Unknown1 { get; set; }
        public byte HitChance { get; set; }
        public MonsterFlags MonsterFlags { get; set; }
        public MonsterElement Element { get; set; }
        public ushort DefeatExperience { get; set; }

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
