namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface INode
    {
        public string Name { get; }
        public string Path { get; }
        public IFolder Parent { get; }
    }
}
