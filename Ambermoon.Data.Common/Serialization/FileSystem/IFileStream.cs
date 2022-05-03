namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IFileStream : IReadOnlyFileStream
    {
        IDisposableDataWriter GetWriter();
    }
}
