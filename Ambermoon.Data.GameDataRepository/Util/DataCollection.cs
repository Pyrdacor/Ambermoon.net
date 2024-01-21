using System.Collections;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Util
{
    using Data;

    public class DataCollection<T> : IEnumerable<T>, IEquatable<DataCollection<T>>, ICloneable where T : IIndexedData, ICloneable
    {

        #region Fields

        private readonly T[] _elements;

        #endregion


        #region Properties

        public int Count { get; }

        #endregion


        #region Indexers

        public T this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        #endregion


        #region Constructors

        public DataCollection()
        {
            Count = 0;
            _elements = Array.Empty<T>();
        }

        private DataCollection(int size)
        {
            Count = size;
            _elements = new T[size];
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public static DataCollection<T> Deserialize(IDataReader dataReader, int size, bool advanced)
        {
            var collection = new DataCollection<T>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (T)T.Deserialize(dataReader, i, advanced);

            return collection;
        }

        #endregion


        #region Methods

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        #endregion


        #region Equality

        public bool Equals(DataCollection<T>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Count == other.Count && _elements.Equals(other._elements);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataCollection<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, _elements);
        }

        public static bool operator ==(DataCollection<T>? left, DataCollection<T>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DataCollection<T>? left, DataCollection<T>? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public DataCollection<T> Copy()
        {
            var copy = new DataCollection<T>(Count);

            for (int i = 0; i < Count; ++i)
                copy._elements[i] = (T)_elements[i].Clone();

            return copy;
        }

        public object Clone() => Copy();

        #endregion

    }

    public class DependentDataCollection<T, D> : IEnumerable<T>, IEquatable<DependentDataCollection<T, D>>, ICloneable
        where T : IIndexedDependentData<D>, IEquatable<T>, ICloneable
        where D : IIndexedData
    {

        #region Fields

        private readonly T[] _elements;

        #endregion


        #region Properties

        public int Count { get; }

        #endregion


        #region Indexers

        public T this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        #endregion


        #region Constructors

        public DependentDataCollection()
        {
            Count = 0;
            _elements = Array.Empty<T>();
        }

        private DependentDataCollection(int size)
        {
            Count = size;
            _elements = new T[size];
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public static DependentDataCollection<T, D> Deserialize(IDataReader dataReader, int size, D providedData, bool advanced)
        {
            var collection = new DependentDataCollection<T, D>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (T)T.Deserialize(dataReader, i, providedData, advanced);

            return collection;
        }

        #endregion


        #region Methods

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        #endregion


        #region Equality

        public bool Equals(DependentDataCollection<T, D>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Count == other.Count && _elements.Equals(other._elements);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DependentDataCollection<T, D>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, _elements);
        }

        public static bool operator ==(DependentDataCollection<T, D>? left, DependentDataCollection<T, D>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DependentDataCollection<T, D>? left, DependentDataCollection<T, D>? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public DependentDataCollection<T, D> Copy()
        {
            var copy = new DependentDataCollection<T, D>(Count);

            for (int i = 0; i < Count; ++i)
                copy._elements[i] = (T)_elements[i].Clone();

            return copy;
        }

        public object Clone() => Copy();

        #endregion

    }
}
