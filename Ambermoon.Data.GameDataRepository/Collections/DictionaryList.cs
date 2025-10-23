using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.ComponentModel;


namespace Ambermoon.Data.GameDataRepository.Collections;

using Data;

public sealed class DictionaryList<T> : ICollection<T>
    where T : class, IIndexed, new()
{

    #region Fields

    private readonly List<T> _list;
    private readonly Dictionary<uint, T> _dictionary;

    #endregion


    #region Properties

    public ICollection<uint> Keys => _dictionary.Keys;

    public int Count => _list.Count;

    public bool IsReadOnly => false;

    #endregion


    #region Indexers

    public T this[uint key]
    {
        get => _dictionary[key];
        set
        {
            T? old = default;
            if (_dictionary.TryGetValue(key, out var item))
            {
                old = item;
                int listIndex = _list.IndexOf(item);
                _list[listIndex] = value;
            }
            else
            {
                _list.Add(value);
                SetupChangeDetection(value);
            }

            _dictionary[key] = value;
            Update(value, old);
        }
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
        _dictionary = _list.ToDictionary(item => item.Index, item => item);
        _list.ForEach(SetupChangeDetection);
    }

    public DictionaryList(IEnumerable<T> enumerable, Func<T, int, uint> keySelector)
    {
        _dictionary = enumerable.Select((e, i) => KeyValuePair.Create(i, e))
            .ToDictionary(item => keySelector(item.Value, item.Key), item => ItemIdSetter(item.Value, keySelector(item.Value, item.Key)));
        _list = new(_dictionary.Values);
        _list.ForEach(SetupChangeDetection);
        return;

        static T ItemIdSetter(T item, uint index)
        {
            if (item is not IMutableIndex mutable) return item;
            if (mutable is ICloneable cloneable)
                mutable = (IMutableIndex)(T)cloneable.Clone();
            mutable.Index = index;
            return (T)mutable;

        }
    }

    #endregion


    #region Methods

    private void SetupChangeDetection(T element)
    {
        if (element is INotifyPropertyChanged notify)
            notify.PropertyChanged += ElementPropertyChanged;
    }

    private void ClearChangeDetection(T element)
    {
        if (element is INotifyPropertyChanged notify)
            notify.PropertyChanged -= ElementPropertyChanged;
    }

    private void Update(T? element, T? old)
    {
        if ((element is null) != (old is null))
            ItemChanged?.Invoke((element ?? old)!.Index);
        else if (element is not null && old is not null)
        {
            if (element.Equals(old)) return;
            ItemChanged?.Invoke(element.Index);
        }
    }

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
        SetupChangeDetection(item);
        Update(item, null);
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

    public void Clear()
    {
        _list.ForEach(item =>
        {
            ClearChangeDetection(item);
            Update(null, item);
        });

        _list.Clear();
        _dictionary.Clear();
    }

    public bool Contains(T item) => _list.Contains(item);

    public bool ContainsKey(uint key) => _dictionary.ContainsKey(key);

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    public int IndexOf(T item) => _list.IndexOf(item);

    public void Insert(int index, T item)
    {
        _dictionary.Add(item.Index, item);
        _list.Insert(index, item);

        SetupChangeDetection(item);
        Update(item, null);
    }

    public bool Remove(uint key)
    {
        if (!_dictionary.Remove(key, out var item)) return false;
        _list.Remove(item);
        ClearChangeDetection(item);
        Update(null, item);
        return true;
    }

    public bool Remove(T item)
    {
        _list.Remove(item);
        if (!_dictionary.Remove(item.Index))
            return false;

        ClearChangeDetection(item);
        Update(null, item);

        return true;
    }

    public void RemoveAt(int index)
    {
        var item = _list[index];
        _dictionary.Remove(item.Index);
        _list.RemoveAt(index);
        ClearChangeDetection(item);
        Update(null, item);
    }

    public T GetAt(int index) => _list[index];

    public void SetAt(int index, T element)
    {
        uint key = GetAt(index).Index;
        this[key] = element;
    }

    public bool TryGetValue(uint key, [MaybeNullWhen(false)] out T value) => _dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

    public Dictionary<uint, T> AsDictionary() => new(_dictionary);

    public void ForEach(Action<T> action) => _list.ForEach(action);

    #endregion


    #region Property Changes

    public event Action<uint>? ItemChanged;

    private void ElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is T element)
        {
            ItemChanged?.Invoke(element.Index);
        }
    }

    #endregion

}


#region Extensions

public static class DictionaryListExtensions
{
    public static DictionaryList<T> ToDictionaryList<T>(this IEnumerable<T> enumerable) where T : class, IIndexed, new() => [.. enumerable];

    public static DictionaryList<T> ToDictionaryList<T>(this IEnumerable<T> enumerable, Func<T, int, uint> keySelector) where T : class, IIndexed, new() => new(enumerable, keySelector);
}

#endregion
