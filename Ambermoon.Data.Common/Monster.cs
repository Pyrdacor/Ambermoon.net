using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Monster : Character
    {
        public MonsterGraphicIndex CombatGraphicIndex { get; set; }
        public MonsterFlags MonsterFlags { get; set; }
        public ushort DefeatExperience { get; set; }
        public Animation[] Animations { get; } = new Animation[8];
        public byte[] UnknownAdditionalBytes { get; set; }

        public class Animation
        {
            public byte[] FrameIndices; // 32 bytes
        }

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
