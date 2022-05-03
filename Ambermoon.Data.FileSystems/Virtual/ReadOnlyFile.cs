using Ambermoon.Data.Serialization.FileSystem;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class ReadOnlyFile : IReadOnlyFile
    {
        readonly DataStream baseStream;
        readonly int streamOffset;
        readonly int streamLength;
        IReadOnlyFileStream stream;

        public ReadOnlyFile(string name, DataStream baseStream, int streamOffset, int streamLength, IReadOnlyFolder parent)
        {
            Name = name;
            this.baseStream = baseStream;
            this.streamOffset = streamOffset;
            this.streamLength = streamLength;
            Parent = parent;
        }

        public IReadOnlyFileStream Stream
        {
            get
            {
                if (stream != null)
                    return stream;

                return stream = new ReadOnlyFileStream(baseStream, streamOffset, streamLength);
            }
        }

        public string Name { get; }

        public string Path => string.IsNullOrEmpty(Parent?.Path) ? Name : Parent.Path + "/" + Name;

        public IReadOnlyFolder Parent { get; }
    }
}
