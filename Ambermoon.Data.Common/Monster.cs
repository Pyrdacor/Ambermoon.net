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
        public byte[] UnknownAdditionalBytes1 { get; set; } // seems to be 16 bytes from 0 to 15
        public byte[] MonsterPalette { get; set; }
        public byte[] UnknownAdditionalBytes2 { get; set; } // 2 bytes
        public uint FrameWidth { get; set; }
        public uint FrameHeight { get; set; }
        public uint MappedFrameWidth { get; set; }
        public uint MappedFrameHeight { get; set; }

        public class Animation
        {
            public int UsedAmount = 0; // 0-32
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
