namespace Ambermoon.Data.Serialization
{
    public interface ITextContainerReader
    {
        void ReadTextContainer(TextContainer textContainer, IDataReader dataReader);
    }
}
