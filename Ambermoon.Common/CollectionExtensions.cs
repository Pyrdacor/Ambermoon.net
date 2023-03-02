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
            int width = array.GetLength(0);
            int height = array.GetLength(1);

            var list = new List<T>(width * height);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                    list.Add(array[x, y]);
            }

            return list;
        }

        public static void AddSorted<T>(this List<T> list, T value)
        {
            int x = list.BinarySearch(value);
            list.Insert((x >= 0) ? x : ~x, value);
        }

        public static void AddSorted<T>(this List<T> list, T value, IComparer<T> comparer)
        {
            int x = list.BinarySearch(value, comparer);
            list.Insert((x >= 0) ? x : ~x, value);
        }
    }
}
