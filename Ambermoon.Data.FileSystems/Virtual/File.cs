using Ambermoon.Data.Serialization.FileSystem;
using System;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class File : IFile
    {
        readonly DataStream baseStream;
        readonly int streamOffset;
        readonly int streamLength;
        IFileStream stream;

        public File(string name, DataStream baseStream, int streamOffset, int streamLength, IFolder parent)
        {
            Name = name;
            this.baseStream = baseStream;
            this.streamOffset = streamOffset;
            this.streamLength = streamLength;
            Parent = parent;
        }

        public IFileStream Stream
        {
            get
            {
                if (this.stream != null)
                    return this.stream;

                var stream = new FileStream(baseStream, streamOffset, streamLength);
                this.stream = stream;
                stream.FileSizeChanged += change => FileSizeChanged?.Invoke(change);

                return stream;
            }
        }

        public string Name { get; }

        public string Path => string.IsNullOrEmpty(Parent?.Path) ? Name : Parent.Path + "/" + Name;

        public IFolder Parent { get; }

        public event Action<int> FileSizeChanged;

        public IReadOnlyFile AsReadOnly(IReadOnlyFolder parent)
        {
            return new ReadOnlyFile(Name, baseStream, streamOffset, streamLength, parent);
        }
    }
}
