using Ambermoon.Data.Serialization.FileSystem;
using System.IO;

namespace Ambermoon.Data.FileSystems
{
    public static class FileSystem
    {
        public static IFileSystem FromOperatingSystemPath(string rootPath)
        {
            return new OperatingSystem.FileSystem(rootPath);
        }

        public static IReadOnlyFileSystem ReadOnlyFromOperatingSystemPath(string rootPath)
        {
            return new OperatingSystem.ReadOnlyFileSystem(rootPath);
        }

        public static IFileSystem LoadVirtual(Stream stream, bool leaveOpen)
        {
            return new Virtual.FileSystem(stream, leaveOpen);
        }

        public static IReadOnlyFileSystem LoadVirtualAsReadOnly(Stream stream, bool leaveOpen)
        {
            return LoadVirtual(stream, leaveOpen).AsReadOnly();
        }

        public static IFileSystem CreateVirtual()
        {
            return new Virtual.FileSystem();
        }

        public static IReadOnlyFileSystem CreateReadOnlyVirtual()
        {
            return CreateVirtual().AsReadOnly();
        }

        public static void SaveVirtual(Stream stream, IFileSystem fileSystem)
        {
            Virtual.FileSystem.Save(stream, fileSystem);
        }

        public static void SaveVirtual(Stream stream, IReadOnlyFileSystem fileSystem)
        {
            Virtual.FileSystem.Save(stream, fileSystem);
        }
    }
}
