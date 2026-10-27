namespace Ambermoon.Mod.Packaging;

/// <summary>
/// A discovered mod: its manifest plus access to its files.
/// </summary>
public sealed class ModPackage : IDisposable
{
    public ModManifest Manifest { get; }
    public IModSource Source { get; }
    public bool IsPacked => Source is PackedModSource;

    ModPackage(ModManifest manifest, IModSource source)
    {
        Manifest = manifest;
        Source = source;
    }

    /// <summary>
    /// Opens a mod folder or a packed .mod file.
    /// </summary>
    public static ModPackage Open(string path)
    {
        IModSource source = Directory.Exists(path) ? new FolderModSource(path) : new PackedModSource(path);

        try
        {
            if (!source.Exists(ModManifest.FileName))
                throw new InvalidDataException($"The mod '{path}' has no {ModManifest.FileName}.");

            var manifest = ModManifest.Parse(source.ReadAllText(ModManifest.FileName));

            if (manifest.HasCode && !source.Exists(manifest.Assembly!))
                throw new InvalidDataException($"The mod assembly '{manifest.Assembly}' was not found in mod '{path}'.");

            return new ModPackage(manifest, source);
        }
        catch
        {
            source.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Finds all mods in the given directory. Each sub folder with a mod.json and
    /// each .mod file is a mod. If both exist for the same mod id, the folder wins
    /// as it is most likely the development version.
    /// </summary>
    public static List<ModPackage> Discover(string modDirectory, Action<string, Exception>? errorHandler = null)
    {
        var mods = new Dictionary<string, ModPackage>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(modDirectory))
            return [];

        var candidates = Directory.GetDirectories(modDirectory)
            .Where(directory => File.Exists(Path.Combine(directory, ModManifest.FileName)))
            .Concat(Directory.GetFiles(modDirectory, "*" + PackedModSource.Extension));

        foreach (var candidate in candidates)
        {
            try
            {
                var mod = Open(candidate);

                if (mods.TryGetValue(mod.Manifest.Id, out var existing))
                {
                    if (existing.IsPacked && !mod.IsPacked)
                    {
                        existing.Dispose();
                        mods[mod.Manifest.Id] = mod;
                    }
                    else
                    {
                        mod.Dispose();
                    }
                }
                else
                {
                    mods.Add(mod.Manifest.Id, mod);
                }
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(candidate, ex);
            }
        }

        return mods.Values.OrderBy(mod => mod.Manifest.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void Dispose() => Source.Dispose();
}
