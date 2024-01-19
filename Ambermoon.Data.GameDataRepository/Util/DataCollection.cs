using System.Collections;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Util
{
    using Data;

    public class DataCollection<T> : IEnumerable<T> where T : IIndexedData
    {
        private readonly T[] _elements;

        public int Count { get; }

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

        public static DataCollection<T> Deserialize(IDataReader dataReader, int size, bool advanced)
        {
            var collection = new DataCollection<T>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (T)T.Deserialize(dataReader, i, advanced);

            return collection;
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public IEnumerator<T> GetEnumerator() => (IEnumerator<T>)_elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        public T this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }
    }

    public class DependentDataCollection<T, D> : IEnumerable<T>
        where T : IIndexedDependentData<D>
        where D : IIndexedData
    {
        private readonly T[] _elements;

        public int Count { get; }

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

        public static DependentDataCollection<T, D> Deserialize(IDataReader dataReader, int size, D providedData, bool advanced)
        {
            var collection = new DependentDataCollection<T, D>(size);

            for (uint i = 0; i < size; i++)
                collection._elements[i] = (T)T.Deserialize(dataReader, i, providedData, advanced);

            return collection;
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            foreach (var item in this)
                item.Serialize(dataWriter, advanced);
        }

        public IEnumerator<T> GetEnumerator() => (IEnumerator<T>)_elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

        public T this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }
    }
}
