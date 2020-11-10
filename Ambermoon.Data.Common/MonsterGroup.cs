using Ambermoon.Data.Serialization;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ambermoon.Data
{
    public class MonsterGroup
    {
        public Monster[,] Monsters { get; } = new Monster[6, 3];

        private MonsterGroup()
        {

        }

        public static MonsterGroup Load(ICharacterManager characterManager, IMonsterGroupReader monsterGroupReader, IDataReader dataReader)
        {
            var monsterGroup = new MonsterGroup();

            monsterGroupReader.ReadMonsterGroup(characterManager, monsterGroup, dataReader);

            return monsterGroup;
        }

        public MonsterGroup Clone()
        {
            var clone = new MonsterGroup();

            for (int row = 0; row < 3; ++row)
            {
                for (int column = 0; column < 6; ++column)
                {
                    clone.Monsters[column, row] = Monsters[column, row]?.Clone();
                }
            }

            return clone;
        }
    }
}
