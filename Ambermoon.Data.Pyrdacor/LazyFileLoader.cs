using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor
{
    public class LazyFileLoader<T, U>(IDataReader dataReader, GameData gameData, Func<T, U> valueProvider)
        where T : class, IFileSpec, new()
    {
        readonly Func<T> loader = () =>
        {
            var item = PADF.Read(dataReader, gameData);

            if (item is T typedItem)
                return typedItem;
            else
                throw new AmbermoonException(ExceptionScope.Data, "Read file had wrong file spec.");
        };
        readonly Func<T, U> valueProvider = valueProvider;
        T? item = null;

        public U Load()
        {
            return valueProvider(item ??= loader());
        }
    }

    public class LazyContainerLoader<T, U>(IDataReader dataReader, GameData gameData, Func<T, U> valueProvider)
        where T : IFileSpec, new()
    {
        readonly Func<Dictionary<ushort, T>> loader = () => PADP.Read<T>(dataReader, gameData);
        readonly Func<T, U> valueProvider = valueProvider;
        Dictionary<ushort, T>? items = null;

        public U Load(ushort key)
        {
            items ??= loader();

            return valueProvider(items[key]);
        }

        public Dictionary<uint, U> LoadAll()
        {
            items ??= loader();

            return items.ToDictionary(i => (uint)i.Key, i => valueProvider(i.Value));
        }
    }
}
