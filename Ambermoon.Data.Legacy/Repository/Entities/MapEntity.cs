using Ambermoon.Data.Legacy.Repository.Util;
using Ambermoon.Data.Serialization;
using System;

#nullable enable

namespace Ambermoon.Data.Legacy.Repository.Entities
{
    public class MapEntity : IIndexedEntity<Map>, INamedEntity<Map>
    {
        public uint Index { get; set; }

        public string Name { get; set; } = string.Empty;

        public TwoDimensionalData<MapTile2DEntity>? Tiles2D { get; set; }

        public TwoDimensionalData<MapTile3DEntity>? Tiles3D { get; set; }     

        public int Width => Tiles3D?.Width ?? Tiles2D?.Width ?? 0;

        public int Height => Tiles3D?.Height ?? Tiles2D?.Height ?? 0;

        public void Resize(int width, int height)
        {
            Tiles2D?.Resize(width, height, () => MapTile2DEntity.Empty);
            Tiles3D?.Resize(width, height, () => MapTile3DEntity.Empty);
        }

        public static IEntity Deserialize(IDataReader dataReader, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public static IEntity<Map> FromGameObject(Map gameObject, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            throw new NotImplementedException();
        }
    }
}
