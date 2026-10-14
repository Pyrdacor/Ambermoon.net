using Ambermoon.Data.Legacy;

namespace Ambermoon.Mod.Tests;

/// <summary>
/// Loads the builtin game versions from versions.dat (like the game does).
/// </summary>
static class BaseGameLoader
{
    public record Version(string Name, string Language, string Info, bool MergeWithPrevious, long Offset, int Size);

    public static string FindVersionsFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var file = Path.Combine(directory.FullName, "Ambermoon.net", "versions.dat");

            if (File.Exists(file))
                return file;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("versions.dat not found.");
    }

    public static List<Version> ReadVersions(BinaryReader reader)
    {
        int ReadWord() => (reader.ReadByte() << 8) | reader.ReadByte();
        int ReadDword() => (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte();

        int count = ReadWord();
        var entries = new List<(string, string, string, bool, int)>();

        for (int i = 0; i < count; i++)
        {
            string name = reader.ReadString();
            string language = reader.ReadString();
            string info = reader.ReadString();
            ReadDword(); // features
            bool merge = reader.ReadByte() != 0;
            int size = ReadDword();
            entries.Add((name, language, info, merge, size));
        }

        long offset = reader.BaseStream.Position;
        var versions = new List<Version>();

        foreach (var entry in entries)
        {
            versions.Add(new Version(entry.Item1, entry.Item2, entry.Item3, entry.Item4, offset, entry.Item5));
            offset += entry.Item5;
        }

        return versions;
    }

    /// <summary>
    /// Loads the original (not advanced) game data in the given language.
    /// </summary>
    public static GameData Load(string language = "English", bool advanced = false)
    {
        using var reader = new BinaryReader(File.OpenRead(FindVersionsFile()));
        var versions = ReadVersions(reader);
        int index = versions.FindIndex(v => v.Language.Equals(language, StringComparison.OrdinalIgnoreCase) &&
            v.Info.Contains("advanced", StringComparison.OrdinalIgnoreCase) == advanced);

        if (index == -1)
            throw new KeyNotFoundException($"No version for {language}.");

        GameData LoadVersion(int versionIndex, Func<Ambermoon.Data.IGameData>? fallback)
        {
            var version = versions[versionIndex];
            reader.BaseStream.Position = version.Offset;
            var buffer = reader.ReadBytes(version.Size);
            var gameData = new GameData(GameData.LoadPreference.PreferExtracted);
            var optionalFiles = fallback == null ? null : new Dictionary<string, char> { { "Intro_texts.amb", 'A' }, { "Extro_texts.amb", 'A' } };
            gameData.LoadFromMemoryZip(new MemoryStream(buffer), fallback, optionalFiles, null);
            return gameData;
        }

        if (!versions[index].MergeWithPrevious)
            return LoadVersion(index, null);

        int baseIndex = index;

        while (versions[baseIndex].MergeWithPrevious)
            baseIndex--;

        return LoadVersion(index, () => LoadVersion(baseIndex, null));
    }
}
