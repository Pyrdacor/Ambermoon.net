using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterReader : CharacterReader, IMonsterReader
    {
        public void ReadMonster(Monster monster, IDataReader dataReader)
        {
            ReadCharacter(monster, dataReader);

            for (int i = 0; i < 8; ++i)
            {
                monster.Animations[i] = new Monster.Animation
                {
                    FrameIndices = dataReader.ReadBytes(32)
                };
            }

            foreach (var animation in monster.Animations)
                animation.UsedAmount = dataReader.ReadByte();

            monster.UnknownAdditionalBytes1 = dataReader.ReadBytes(16); // TODO
            monster.MonsterPalette = dataReader.ReadBytes(32);
            monster.UnknownAdditionalBytes2 = dataReader.ReadBytes(2); // TODO
            monster.FrameWidth = dataReader.ReadWord();
            monster.FrameHeight = dataReader.ReadWord();
            monster.MappedFrameWidth = dataReader.ReadWord();
            monster.MappedFrameHeight = dataReader.ReadWord();
        }
    }
}
