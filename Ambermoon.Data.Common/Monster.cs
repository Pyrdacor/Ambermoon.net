using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ambermoon.Data
{
    [Serializable]
    public class Monster : Character
    {
        public MonsterGraphicIndex CombatGraphicIndex { get; set; }
        public uint Morale { get; set; }
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
        public Graphic CombatGraphic { get; set; }

        [Serializable]
        public class Animation
        {
            public int UsedAmount = 0; // 0-32
            public byte[] FrameIndices; // 32 bytes
        }

        private Monster()
            : base(CharacterType.Monster)
        {

        }

        public static Monster Load(uint index, IMonsterReader monsterReader, IDataReader dataReader)
        {
            var monster = new Monster
            {
                Index = index
            };

            monsterReader.ReadMonster(monster, dataReader);

            return monster;
        }

        public Monster Clone()
        {
            // Note: Binary serialization is slow to create a clone
            // but normally cloning is only done once when a fight starts.
            // So this doesn't matter much and is easier to implement
            // than the alternatives.
            using var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return (Monster)formatter.Deserialize(stream);
        }

        /// <summary>
        /// Gets the animation frame index for a given animation type and the
        /// total animation ticks of the animation.
        /// 
        /// If the animation is over or there are no frames at all this
        /// method will return null to state that there is no frame.
        /// </summary>
        public int? GetAnimationFrameIndex(MonsterAnimationType animationType, uint animationTicks, uint ticksPerFrame)
        {
            var animation = Animations[(int)animationType];

            if (animation.UsedAmount == 0)
                return null;

            uint frameIndex = animationTicks / ticksPerFrame;

            if (frameIndex >= animation.UsedAmount)
                return null;

            return animation.FrameIndices[frameIndex];
        }

        public int GetAnimationFrameCount(MonsterAnimationType animationType) => Animations[(int)animationType].UsedAmount;

        public int[] GetAnimationFrameIndices(MonsterAnimationType animationType)
        {
            var animation = Animations[(int)animationType];
            return animation.FrameIndices.Take(animation.UsedAmount).Select(b => (int)b).ToArray();
        }
    }
}
