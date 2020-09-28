using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

        public static List<T> ToList<T>(this T[,] array)
        {
            var list = new List<T>(array.GetLength(0) * array.GetLength(1));

            foreach (var elem in array)
                list.Add(elem);

            return list;
        }
    }
}
