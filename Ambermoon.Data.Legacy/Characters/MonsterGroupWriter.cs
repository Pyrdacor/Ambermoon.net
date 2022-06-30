using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterGroupWriter
    {
        public static void WriteMonsterGroup(MonsterGroup monsterGroup, IDataWriter dataWriter)
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 6; ++c)
                {
                    dataWriter.Write((ushort)(monsterGroup.Monsters[c, r]?.Index ?? 0));
                }
            }
        }
    }
}
