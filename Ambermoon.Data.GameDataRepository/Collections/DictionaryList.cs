using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Ambermoon.Data.GameDataRepository.Collections
{
    using Data;

    public sealed class DictionaryList<T> : IList<T>, IReadOnlyList<T>, IList
        where T : IIndexed, new()
    {

        #region Fields

        private readonly List<T> _list;
        private readonly Dictionary<uint, T> _dictionary;

        #endregion


        #region Properties

        public ICollection<uint> Keys => _dictionary.Keys;

        public int Count => _list.Count;

        public bool IsReadOnly { get; } = false;

        public bool IsSynchronized => (_list as IList).IsSynchronized;

        public object SyncRoot => (_list as IList).SyncRoot;

        public bool IsFixedSize => (_list as IList).IsFixedSize;

        #endregion


        #region Indexers

        public T this[uint key]
        {
            get => _dictionary[key];
            set
            {
                if (_dictionary.TryGetValue(key, out var item))
                {
                    int listIndex = _list.IndexOf(item);
                    _list[listIndex] = value;
                }
                else
                {
                    _list.Add(value);
                }

                _dictionary[key] = value;
            }
        }
        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (value.Index != _list[index].Index)
                {
                    // We replace an item in the list with one with a different index.
                    // This is ok as long as the new index is not already taken.
                    if (_dictionary.ContainsKey(value.Index))
                        throw new ArgumentException("An item with the same key is already present inside the " + nameof(DictionaryList<T>) + ".");
                }
                _dictionary.Remove(_list[index].Index);
                _dictionary[_list[index].Index] = value;
                _list[index] = value;
            }
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value!;
        }

        #endregion


        #region Constructors

        public DictionaryList()
        {
            _list = new();
            _dictionary = new();
        }

        public DictionaryList(int capacity)
        {
            _list = new(capacity);
            _dictionary = new(capacity);
        }

        public DictionaryList(IEnumerable<T> enumerable)
        {
            _list = new(enumerable);
            _dictionary = _list.ToDictionary(item => item.Index, Item => Item);
        }

        #endregion


        #region Methods

        public T Create(uint? index = null)
        {
            T obj = new();

            if (obj is IMutableIndex mutable)
                mutable.Index = index ?? (Count == 0 ? 1 : Keys.Max() + 1);
            else
                throw new InvalidOperationException($"Unable to create objects of type {typeof(T)}.");

            return obj;
        }

        public T CreateClone(T source, uint? index = null)
        {
            if (source is not ICloneable cloneable)
                throw new InvalidOperationException($"Unable to clone objects of type {typeof(T)}.");

            var obj = (T)cloneable.Clone();

            if (obj is IMutableIndex mutable)
                mutable.Index = index ?? (Count == 0 ? 1 : Keys.Max() + 1);
            else
                throw new InvalidOperationException($"Unable to create objects of type {typeof(T)}.");

            return obj;
        }

        public void Add(T item)
        {
            _dictionary.Add(item.Index, item);
            _list.Add(item);
        }

        public T AddNew(uint? index)
        {
            var newItem = Create(index);
            Add(newItem);
            return newItem;
        }

        public T AddClone(T source, uint index)
        {
            var newItem = CreateClone(source, index);
            Add(newItem);
            return newItem;
        }

        public int Add(object? value)
        {
            Add((T)value!);
            return Count - 1;
        }

        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }

        public bool Contains(T item) => _list.Contains(item);

        public bool Contains(object? value) => Contains((T)value!);

        public bool ContainsKey(uint key) => _dictionary.ContainsKey(key);

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            (_list as IList).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(T item) => _list.IndexOf(item);

        public int IndexOf(object? value) => (_list as IList).IndexOf((T)value!);

        public void Insert(int index, T item)
        {
            _dictionary.Add(item.Index, item);
            _list.Insert(index, item);
        }

        public void Insert(int index, object? value) => Insert(index, (T)value!);

        public bool Remove(uint key)
        {
            if (!_dictionary.Remove(key, out var item)) return false;
            _list.Remove(item);
            return true;
        }

        public bool Remove(T item)
        {
            _list.Remove(item);
            return _dictionary.Remove(item.Index);
        }

        public void Remove(object? value) => Remove((T)value!);

        public void RemoveAt(int index)
        {
            _dictionary.Remove(_list[index].Index);
            _list.RemoveAt(index);
        }

        public bool TryGetValue(uint key, [MaybeNullWhen(false)] out T value) => _dictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public Dictionary<uint, T> AsDictionary() => new(_dictionary);

        #endregion

    }


    #region Extensions

    public static class DictionaryListExtensions
    {
        public static DictionaryList<T> ToDictionaryList<T>(this IEnumerable<T> enumerable) where T : IIndexed, new() => new DictionaryList<T>(enumerable);
    }

    #endregion

}
