using System.Collections;
using System.ComponentModel;

namespace Ambermoon.Data.GameDataRepository.Collections
{
    using Data;
    using Serialization;

    public class DataCollection<TElement> : IEnumerable<TElement>, IEquatable<DataCollection<TElement>>, ICloneable
        where TElement : IIndexedData, IEquatable<TElement>, new()
    {

        #region Fields

        private readonly TElement[] _elements;

        #endregion


        #region Properties

        public int Count { get; }

        #endregion


        #region Indexers

        public TElement this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        #endregion


        #region Constructors

        public DataCollection()
        {
            Count = 0;
            _elements = Array.Empty<TElement>();
        }

        internal DataCollection(int size)
        {
            Count = size;
            _elements = new TElement[size];

            for (int i = 0; i < size; ++i)
                _elements[i] = new();
        }

        internal DataCollection(int size, Func<int, TElement> valueProvider)
        {
            Count = size;
            _elements = new TElement[size];

            for (int i = 0; i < size; ++i)
                _elements[i] = valueProvider(i);
        }

        #endregion


        #region Methods

        public TElement Get(int index) => _elements[index];

        public void Set(int index, TElement element)
        {
            if (_elements[index].Equals(element)) return;
            if (_elements[index] is INotifyPropertyChanged oldNotify)
                oldNotify.PropertyChanged -= ElementPropertyChanged;
            _elements[index] = element;
            if (element is INotifyPropertyChanged newNotify)
                newNotify.PropertyChanged += ElementPropertyChanged;
            ItemChanged?.Invoke(index);
        }

        public IEnumerator<TElement> GetEnumerator() => ((IEnumerable<TElement>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public static DataCollection<TElement> Deserialize(IDataReader dataReader, int size, bool advanced)
        {
            var collection = new DataCollection<TElement>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (TElement)TElement.Deserialize(dataReader, i, advanced);

            return collection;
        }

        #endregion


        #region Equality

        public bool Equals(DataCollection<TElement>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Count == other.Count && _elements.Equals(other._elements);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DataCollection<TElement>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, _elements);
        }

        public static bool operator ==(DataCollection<TElement>? left, DataCollection<TElement>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DataCollection<TElement>? left, DataCollection<TElement>? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        private static TElement CloneElement(TElement element)
        {
            if (element is ICloneable cloneable)
                return (TElement)cloneable.Clone();

            return element;
        }

        public DataCollection<TElement> Copy(bool cloneElements = true)
        {
            var copy = new DataCollection<TElement>(Count);

            for (int i = 0; i < Count; ++i)
                copy._elements[i] = cloneElements ? CloneElement(_elements[i]) : _elements[i];

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event Action<int>? ItemChanged;

        private void ElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is TElement element)
            {
                var index = _elements.ToList().IndexOf(element);

                if (index >= 0)
                    ItemChanged?.Invoke(index);
            }
        }

        #endregion

    }

    public class DependentDataCollection<TElement, TDependency> : IEnumerable<TElement>, IEquatable<DependentDataCollection<TElement, TDependency>>, ICloneable
        where TElement : IIndexedDependentData<TDependency>, IEquatable<TElement>, new()
        where TDependency : IIndexedData
    {

        #region Fields

        private readonly TElement[] _elements;

        #endregion


        #region Properties

        public int Count { get; }

        #endregion


        #region Indexers

        public TElement this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        #endregion


        #region Constructors

        public DependentDataCollection()
        {
            Count = 0;
            _elements = Array.Empty<TElement>();
        }

        internal DependentDataCollection(int size)
        {
            Count = size;
            _elements = new TElement[size];

            for (int i = 0; i < size; ++i)
                _elements[i] = new();
        }

        internal DependentDataCollection(int size, Func<int, TElement> valueProvider)
        {
            Count = size;
            _elements = new TElement[size];

            for (int i = 0; i < size; ++i)
                _elements[i] = valueProvider(i);
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public static DependentDataCollection<TElement, TDependency> Deserialize(IDataReader dataReader, int size, TDependency providedData, bool advanced)
        {
            var collection = new DependentDataCollection<TElement, TDependency>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (TElement)TElement.Deserialize(dataReader, i, providedData, advanced);

            return collection;
        }

        #endregion


        #region Methods

        public TElement Get(int index) => _elements[index];

        public void Set(int index, TElement element)
        {
            if (_elements[index].Equals(element)) return;
            if (_elements[index] is INotifyPropertyChanged oldNotify)
                oldNotify.PropertyChanged -= ElementPropertyChanged;
            _elements[index] = element;
            if (element is INotifyPropertyChanged newNotify)
                newNotify.PropertyChanged += ElementPropertyChanged;
            ItemChanged?.Invoke(index);
        }

        private void ElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is TElement element)
            {
                var index = _elements.ToList().IndexOf(element);

                if (index >= 0)
                    ItemChanged?.Invoke(index);
            }
        }

        public IEnumerator<TElement> GetEnumerator() => ((IEnumerable<TElement>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        #endregion


        #region Equality

        public bool Equals(DependentDataCollection<TElement, TDependency>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Count == other.Count && _elements.Equals(other._elements);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DependentDataCollection<TElement, TDependency>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, _elements);
        }

        public static bool operator ==(DependentDataCollection<TElement, TDependency>? left, DependentDataCollection<TElement, TDependency>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DependentDataCollection<TElement, TDependency>? left, DependentDataCollection<TElement, TDependency>? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        private static TElement CloneElement(TElement element)
        {
            if (element is ICloneable cloneable)
                return (TElement)cloneable.Clone();

            return element;
        }

        public DependentDataCollection<TElement, TDependency> Copy(bool cloneElements = true)
        {
            var copy = new DependentDataCollection<TElement, TDependency>(Count);

            for (int i = 0; i < Count; ++i)
                copy._elements[i] = cloneElements ? CloneElement(_elements[i]) : _elements[i];

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event Action<int>? ItemChanged;

        #endregion

    }
}
