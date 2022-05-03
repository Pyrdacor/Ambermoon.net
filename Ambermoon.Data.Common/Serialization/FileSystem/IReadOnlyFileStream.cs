namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IReadOnlyFileStream
    {
        IDisposableDataReader GetReader();
        int Size { get; }
    }
}
