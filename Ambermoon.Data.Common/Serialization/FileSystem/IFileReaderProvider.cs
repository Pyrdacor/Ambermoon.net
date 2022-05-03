namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IFileReaderProvider
    {
        IReadOnlyFileStream GetFileReader(string path);
    }
}
