using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Monster : Character
    {
        public ushort CombatGraphicIndex { get; set; }
        public MonsterFlags MonsterFlags { get; set; }
        public ushort DefeatExperience { get; set; }
        public byte[] UnknownAdditionalBytes { get; set; }

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
