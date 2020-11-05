using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterReader : CharacterReader, IMonsterReader
    {
        public void ReadMonster(Monster monster, IDataReader dataReader)
        {
            ReadCharacter(monster, dataReader);

            foreach (var animation in monster.Animations)
                animation.FrameIndices = dataReader.ReadBytes(32);

            // TODO: monsters have some additional data (maybe palette index changes?)
            monster.UnknownAdditionalBytes = dataReader.ReadToEnd();
        }
    }
}
