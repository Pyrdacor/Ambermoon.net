namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IReadOnlyNode
    {
        public string Name { get; }
        public string Path { get; }
        public IReadOnlyFolder Parent { get; }
    }
}
