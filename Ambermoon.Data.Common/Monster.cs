using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Linq;

namespace Ambermoon.Data
{
    [Serializable]
    [AutoClone]
    public partial class Monster : Character, IAutoClone<Monster>
    {
        public MonsterGraphicIndex CombatGraphicIndex { get; set; }
        public uint Morale { get; set; }
        public ushort DefeatExperience { get; set; }
        public Animation[] Animations { get; init; } = new Animation[8];
        public byte[] AtariPalette { get; set; } // not used
        public byte[] MonsterPalette { get; set; }
        public byte[] UnknownAdditionalBytes2 { get; set; } // 2 bytes
        public uint FrameWidth { get; set; }
        public uint FrameHeight { get; set; }
        public uint MappedFrameWidth { get; set; }
        public uint MappedFrameHeight { get; set; }
        public Graphic CombatGraphic { get; set; }

        public partial Monster DeepClone();

        [Serializable]
        public class Animation
        {
            public int UsedAmount = 0; // 0-32
            public byte[] FrameIndices; // 32 bytes

			public Animation()
            {

            }

			public Animation(Animation other)
            {
                UsedAmount = other.UsedAmount;
                FrameIndices = other.FrameIndices;
            }
		}

        public Monster()
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
