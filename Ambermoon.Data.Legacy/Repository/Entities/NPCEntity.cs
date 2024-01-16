using Ambermoon.Data.Serialization;
using System;

#nullable enable

namespace Ambermoon.Data.Legacy.Repository.Entities
{
    public class NpcEntity : IIndexedEntity<NPC>, INamedEntity<NPC>
    {
        public uint Index { get; set; }

        public string Name { get; set; } = string.Empty;

        public static IEntity Deserialize(IDataReader dataReader, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public static IEntity<NPC> FromGameObject(NPC gameObject, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            throw new NotImplementedException();
        }
    }
}
