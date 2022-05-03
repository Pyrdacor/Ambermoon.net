namespace Ambermoon.Data.Serialization.FileSystem
{
    public interface IFile : INode
    {
        public IFileStream Stream { get; }
    }
}
