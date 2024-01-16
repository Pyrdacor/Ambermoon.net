using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Repository.Entities
{
    public interface IEntity
    {
        /// <summary>
        /// Serializes the entity to a data writer.
        /// </summary>
        /// <param name="dataWriter">Writer to store the serialized data.</param>
        /// <param name="gameData">Game data for additional required data.</param>
        void Serialize(IDataWriter dataWriter, IGameData gameData);

        /// <summary>
        /// Deserializes an entity from a data reader.
        /// </summary>
        /// <param name="dataReader">Reader which provides the entity data.</param>
        /// <param name="gameData">Game data for additional required data.</param>
        /// <returns></returns>
        static abstract IEntity Deserialize(IDataReader dataReader, IGameData gameData);
    }

    public interface IEntity<TGameObject> : IEntity
    {       
        /// <summary>
        /// Creates an entity from a game object.
        /// </summary>
        /// <param name="gameObject">Game object which provides information for the entity.</param>
        /// <param name="gameData">Game data for additional required data.</param>
        /// <returns></returns>
        static abstract IEntity<TGameObject> FromGameObject(TGameObject gameObject, IGameData gameData);
    }

    public interface IIndexedEntity
    {
        uint Index { get; set; }
    }

    public interface IIndexedEntity<TGameObject> : IIndexedEntity, IEntity<TGameObject>
    {
    }

    public interface INamedEntity<TGameObject> : IEntity<TGameObject>
    {
        string Name { get; set; }
    }

    public interface IBackConversionEntity<TGameObject> : IEntity<TGameObject>
    {
        /// <summary>
        /// Creates a game object from an entity.
        /// </summary>
        /// <param name="gameData">Game data for additional required data.</param>
        /// <returns></returns>
        TGameObject ToGameObject(IGameData gameData);
    }

    public static class EntityExtensions
    {
        public static T ToEntity<TGameObject, T>(this TGameObject gameObject, IGameData gameData) where T : IEntity<TGameObject>
        {
            return (T)T.FromGameObject(gameObject, gameData);
        }
    }
}
