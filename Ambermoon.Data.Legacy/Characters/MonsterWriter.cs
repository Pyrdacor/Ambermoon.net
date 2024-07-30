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

            foreach (var animation in monster.Animations)
                dataWriter.Write((byte)animation.UsedAmount);

            dataWriter.Write(monster.AtariPalette);
            dataWriter.Write(monster.MonsterPalette);
			dataWriter.Write(monster.AlternateAnimationBits);
			dataWriter.Write(monster.PaddingByte);
            dataWriter.Write((ushort)monster.FrameWidth);
            dataWriter.Write((ushort)monster.FrameHeight);
            dataWriter.Write((ushort)monster.MappedFrameWidth);
            dataWriter.Write((ushort)monster.MappedFrameHeight);
        }
    }
}
