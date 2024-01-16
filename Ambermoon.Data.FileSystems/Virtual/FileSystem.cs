using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.FileSystems.Virtual
{
    internal class FileSystem : IFileSystem
    {
        const string Magic = "PYFS";
        const int MajorVersion = 0;
        const int MinorVersion = 0;
        const string RootFolderName = "";
        DataStream dataStream;
        readonly Folder rootFolder;
        readonly Dictionary<string, File> files = new Dictionary<string, File>();
        readonly List<File> filesInOrder = new List<File>();

        struct FileInfo
        {
            public int Size;
            public string Path;
        }

        public FileSystem()
        {
            rootFolder = new Folder(RootFolderName, null);
            dataStream = new DataStream();
        }

        public FileSystem(Stream stream, bool leaveOpen)
        {
            rootFolder = new Folder(RootFolderName, null);
            Load(stream, leaveOpen);
        }

        static void WriteHeader(DataWriter writer)
        {
            writer.WriteWithoutLength(Magic);
            writer.Write((ushort)MajorVersion);
            writer.Write((ushort)MinorVersion);
            uint flags = 0; // not used yet
            writer.Write(flags);
        }

        public static void Save(Stream stream, IFileSystem fileSystem)
        {
            var writer = new DataWriter();

            WriteHeader(writer);

            var allFiles = fileSystem.GetAllFiles().ToList();

            writer.Write((ushort)allFiles.Count);

            foreach (var file in allFiles)
            {
                writer.Write((ushort)file.Stream.Size);
                writer.WriteNullTerminated(file.Path);
            }

            foreach (var file in allFiles)
            {
                writer.Write(file.Stream.GetReader().ReadToEnd());
            }

            writer.CopyTo(stream);
        }

        public static void Save(Stream stream, IReadOnlyFileSystem fileSystem)
        {
            var writer = new DataWriter();

            WriteHeader(writer);

            var allFiles = fileSystem.GetAllFiles().ToList();

            writer.Write((ushort)allFiles.Count);

            foreach (var file in allFiles)
            {
                writer.Write((ushort)file.Stream.Size);
                writer.WriteNullTerminated(file.Path);
            }

            foreach (var file in allFiles)
            {
                writer.Write(file.Stream.GetReader().ReadToEnd());
            }

            writer.CopyTo(stream);
        }

        void Load(Stream stream, bool leaveOpen)
        {
            // First there is a magic string: "PYFS" (Pyrdacor's filesystem)
            // Then the file structure version follows as two words (major and minor)
            // Then there is a flag dword.
            // Then the amount of files follows as a word.
            // For each file there is a info entry with the full path (relative to root) and file size.
            // Then for each file the file data is stored.

            using var reader = new StreamedDataReader(stream, leaveOpen);

            if (reader.ReadString(4) != Magic)
                throw new InvalidDataException("Invalid magic string.");

            int major = reader.ReadWord();
            int minor = reader.ReadWord();

            if (major > MajorVersion || (major == MajorVersion && minor > MinorVersion))
                throw new NotSupportedException("File version can't be read by this reader.");

            uint flags = reader.ReadDword(); // not used yet, should be 0

            if (flags != 0)
                throw new InvalidDataException("Invalid flags value.");

            int numFiles = reader.ReadWord();
            var fileInfos = new FileInfo[numFiles];

            for (int i = 0; i < numFiles; ++i)
                fileInfos[i] = new FileInfo { Size = reader.ReadWord(), Path = reader.ReadNullTerminatedString(Encoding.UTF8) };

            dataStream = new DataStream(reader.ReadToEnd());

            int offset = 0;

            for (int i = 0; i < numFiles; ++i)
            {
                AddFile(fileInfos[i].Path, offset, fileInfos[i].Size);
                offset += fileInfos[i].Size;
            }

            void AddFile(string path, int offset, int length)
            {
                var parts = GetPathParts(path);
                var parent = GetNode(parts, 0, rootFolder, true) as Folder;

                if (parent == null)
                    parent = CreateFolder(string.Join("/", parts.Take(parts.Length - 1))) as Folder;

                if (parent == null)
                    throw new Exception("Failed to add file.");

                var file = new File(parts[^1], dataStream, offset, length, parent);

                parent.AddFile(file.Name, file);
                files.Add(file.Path, file);
                filesInOrder.Add(file);
                file.FileSizeChanged += change =>
                {
                    FileSizeChanged(file, change);
                };
            }
        }

        public bool MemoryFileSystem => true;

        string[] GetPathParts(string path)
        {
            return path.Split("/");
        }

        public INode GetNode(string path)
        {
            var parts = GetPathParts(path);
            return GetNode(parts, 0, rootFolder, false);
        }

        INode GetNode(string[] pathParts, int currentIndex, IFolder parent, bool getParent)
        {
            string name = pathParts[currentIndex];

            int endIndex = getParent ? pathParts.Length - 2 : pathParts.Length - 1;

            if (currentIndex == endIndex)
            {
                if (parent.Files.TryGetValue(name, out var file))
                    return file;

                if (parent.Folders.TryGetValue(name, out var folder))
                    return folder;                

                return null;
            }
            else
            {
                if (!parent.Folders.TryGetValue(name, out var folder))
                    return null;

                return GetNode(pathParts, currentIndex + 1, folder, getParent);
            }
        }

        public IFile GetFile(string path)
        {
            return GetNode(path) as IFile;
        }

        public IFolder GetFolder(string path)
        {
            return GetNode(path) as IFolder;
        }

        public IFolder CreateFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return rootFolder;

            var parts = GetPathParts(path);
            var parent = GetNode(parts, 0, rootFolder, true) as Folder;

            if (parent == null)
                parent = CreateFolder(string.Join("/", parts.Take(parts.Length - 1))) as Folder;

            if (parent == null)
                return null;

            return parent.AddFolder(parts[^1]);
        }

        public IFile CreateEmptyFile(string path)
        {
            return CreateFile(path, null);
        }

        public IFile CreateFile(string path, byte[] data)
        {
            var parts = GetPathParts(path);
            var parent = GetNode(parts, 0, rootFolder, true) as Folder;

            if (parent == null)
                parent = CreateFolder(string.Join("/", parts.Take(parts.Length - 1))) as Folder;

            if (parent == null)
                return null;

            int offset = dataStream.Position;

            dataStream.AppendBytes(data);

            var file = new File(parts[^1], dataStream, offset, data.Length, parent);

            parent.AddFile(file.Name, file);
            files.Add(file.Path, file);
            filesInOrder.Add(file);
            file.FileSizeChanged += change =>
            {
                FileSizeChanged(file, change);
            };

            return file;
        }

        void FileSizeChanged(File file, int change)
        {
            bool adjust = false;

            foreach (var f in filesInOrder)
            {
                if (adjust)
                {
                    var stream = f.Stream as FileStream;
                    stream.AdjustOffset(stream.Offset + change);
                    continue;
                }

                if (f == file)
                {
                    adjust = true;
                    continue;
                }
            }
        }

        public IReadOnlyFileSystem AsReadOnly() => new ReadOnlyFileSystem(rootFolder, files);

        public IReadOnlyFileStream GetFileReader(string path)
        {
            return GetFile(path).Stream;
        }

        public IEnumerable<IFile> GetAllFiles() => filesInOrder;
    }
}
