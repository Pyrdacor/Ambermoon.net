using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterWriter : CharacterWriter
    {
        public void WriteMonster(Monster monster, IDataWriter dataWriter)
        {
            WriteCharacter(monster, dataWriter);

            foreach (var animation in monster.Animations)
                dataWriter.Write(animation.FrameIndices);

            // TODO: monsters have some additional data
            dataWriter.Write(monster.UnknownAdditionalBytes);
        }
    }
}
