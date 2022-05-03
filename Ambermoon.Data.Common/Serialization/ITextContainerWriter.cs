namespace Ambermoon.Data.Serialization
{
    public interface ITextContainerWriter
    {
        void WriteTextContainer(TextContainer textContainer, IDataWriter dataWriter);
    }
}
