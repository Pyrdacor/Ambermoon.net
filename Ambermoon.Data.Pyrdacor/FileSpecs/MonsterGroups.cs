using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    internal class MonsterGroups : IFileSpec
    {
        public string Magic => "MOG";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();
        MonsterGroup? monsterGroup = null;

        public MonsterGroup MonsterGroup => monsterGroup!;

        public MonsterGroups()
        {

        }

        public MonsterGroups(MonsterGroup monsterGroup)
        {
            this.monsterGroup = monsterGroup;
        }

        public void Read(IDataReader dataReader, uint _, GameData gameData)
        {
            monsterGroup = MonsterGroup.Load(gameData.CharacterManager ??
                throw new AmbermoonException(ExceptionScope.Application, "Character manager was not created before monster group loading."),
                new MonsterGroupReader(), dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (monsterGroup == null)
                throw new AmbermoonException(ExceptionScope.Application, "Chest data was null when trying to write it.");

            MonsterGroupWriter.WriteMonsterGroup(monsterGroup, dataWriter);
        }
    }
}
