namespace Ambermoon.Data.GameDataRepository
{
    using Ambermoon.Data.Serialization;
    using Data;

    public class TextList : List<string>
    {
        public TextList()
        {
        }

        public TextList(IEnumerable<string> texts)
            : base(texts)
        {
        }
    }

    public class TextList<T> : TextList, IIndexedDependentData<T>
        where T : IIndexedData
    {
        public TextList(T associatedItem)
        {
            AssociatedItem = associatedItem;
            Index = associatedItem.Index;
        }

        public TextList(T associatedItem, IEnumerable<string> texts)
            : base(texts)
        {
            AssociatedItem = associatedItem;
            Index = associatedItem.Index;
        }

        public T AssociatedItem { get; }
        public uint Index { get; private set; }

        public static IIndexedDependentData<T> Deserialize(IDataReader dataReader, uint index, T providedData, bool advanced)
        {
            var textList = (TextList<T>)Deserialize(dataReader, providedData, advanced);
            textList.Index = index;
            return textList;
        }

        public static IDependentData<T> Deserialize(IDataReader dataReader, T providedData, bool advanced)
        {
            return new TextList<T>(providedData, Legacy.Serialization.TextReader.ReadTexts(dataReader));
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            Legacy.Serialization.TextWriter.WriteTexts(dataWriter, this);
        }
    }
}
