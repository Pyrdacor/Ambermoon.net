using System.IO.Compression;

namespace Ambermoon.Mod.Packaging;

/// <summary>
/// Provides the files of a mod. A mod is either a plain folder
/// (used while developing) or a packed .mod file (a zip archive).
/// All paths use '/' as separator and are relative to the mod root.
/// </summary>
public interface IModSource : IDisposable
{
    /// <summary>
    /// Folder or .mod file path.
    /// </summary>
    string Location { get; }
    bool Exists(string path);
    byte[] ReadAllBytes(string path);
    IEnumerable<string> ListFiles(string directory);
}

public static class ModSourceExtensions
{
    public static string ReadAllText(this IModSource source, string path)
        => System.Text.Encoding.UTF8.GetString(source.ReadAllBytes(path)).TrimStart('﻿');

    internal static string Normalize(string path) => path.Replace('\\', '/').Trim('/');
}

public sealed class FolderModSource(string directory) : IModSource
{
    public string Location { get; } = Path.GetFullPath(directory);

    string ToFullPath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Location, ModSourceExtensions.Normalize(path)));

        if (!fullPath.StartsWith(Location, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Path '{path}' is outside of the mod.");

        return fullPath;
    }

    public bool Exists(string path) => File.Exists(ToFullPath(path));

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(ToFullPath(path));

    public IEnumerable<string> ListFiles(string directory)
    {
        var fullPath = ToFullPath(directory);

        if (!Directory.Exists(fullPath))
            return [];

        return Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Location, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.Ordinal);
    }

    public void Dispose()
    {
    }
}

public sealed class PackedModSource : IModSource
{
    public const string Extension = ".mod";
    readonly ZipArchive archive;
    readonly Dictionary<string, ZipArchiveEntry> entries;

    public string Location { get; }

    public PackedModSource(string file)
        : this(File.OpenRead(file), file)
    {
    }

    public PackedModSource(Stream stream, string location)
    {
        Location = location;
        archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        entries = archive.Entries
            .Where(entry => !entry.FullName.EndsWith('/'))
            .ToDictionary(entry => ModSourceExtensions.Normalize(entry.FullName), StringComparer.Ordinal);
    }

    public bool Exists(string path) => entries.ContainsKey(ModSourceExtensions.Normalize(path));

    public byte[] ReadAllBytes(string path)
    {
        if (!entries.TryGetValue(ModSourceExtensions.Normalize(path), out var entry))
            throw new FileNotFoundException($"File '{path}' was not found in mod '{Location}'.");

        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    public IEnumerable<string> ListFiles(string directory)
    {
        var prefix = ModSourceExtensions.Normalize(directory);

        if (prefix.Length != 0)
            prefix += "/";

        return entries.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal);
    }

    public void Dispose() => archive.Dispose();
}
