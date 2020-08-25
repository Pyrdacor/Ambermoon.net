using System.Collections.Generic;

namespace Ambermoon
{
    public static class CollectionExtensions
    {
        public static void SafeAdd<TKey, TCollectionItem>(this Dictionary<TKey, List<TCollectionItem>> dictionary, TKey key, TCollectionItem value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary[key] = new List<TCollectionItem> { value };
            else
                dictionary[key].Add(value);
        }
    }
}
