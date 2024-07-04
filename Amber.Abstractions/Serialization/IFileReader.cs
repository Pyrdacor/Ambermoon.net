namespace Amber.Serialization
{
    public interface IFileReader
    {
        IFileContainer ReadRawFile(string name, Stream stream);
    }
}
