namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IReadOnlyFile : IReadOnlyNode
    {
        public IReadOnlyFileStream Stream { get; }
    }
}
