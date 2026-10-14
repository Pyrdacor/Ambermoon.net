using System.Reflection;
using System.Runtime.Loader;
using Ambermoon.Data;
using Ambermoon.Mod.Assets;
using Ambermoon.Mod.Packaging;

namespace Ambermoon.Mod;

/// <summary>
/// A loaded and active mod. This is the entry point for the frontend:
///
/// 1. Load the base game data.
/// 2. If <see cref="ModManifest.HasCode"/>, check <see cref="IsTrusted"/> and ask the player if needed.
/// 3. Call <see cref="Load"/> which applies the assets and initializes the mod code.
/// 4. Wrap the savegame manager with <see cref="WrapSavegameManager"/>.
/// 5. After creating the game, call <see cref="AttachGame"/>.
/// </summary>
public sealed class ModRuntime
{
    readonly ModHookDispatcher dispatcher;

    public ModPackage Package { get; }
    public ModManifest Manifest => Package.Manifest;
    public ModApi Api { get; }
    /// <summary>
    /// The code part of the mod (null for asset-only mods).
    /// </summary>
    public ModBase? Mod { get; }
    public IReadOnlyList<string> Warnings { get; }
    /// <summary>
    /// Song number to file path inside the mod.
    /// </summary>
    public IReadOnlyDictionary<int, string> MusicFiles { get; }

    ModRuntime(ModPackage package, ModApi api, ModBase? mod, List<string> warnings)
    {
        Package = package;
        Api = api;
        Mod = mod;
        Warnings = warnings;
        MusicFiles = ModAssetApplier.GetMusicFiles(package.Source);
        dispatcher = new ModHookDispatcher(mod, api);
    }

    /// <summary>
    /// Returns the hash of the mod assembly or null for asset-only mods.
    /// </summary>
    public static string? GetAssemblyHash(ModPackage package)
        => package.Manifest.HasCode ? ModTrustStore.ComputeHash(package.Source.ReadAllBytes(package.Manifest.Assembly!)) : null;

    /// <summary>
    /// Checks if the mod can be loaded without asking the player.
    /// </summary>
    public static bool IsTrusted(ModPackage package, ModTrustStore trustStore)
    {
        var hash = GetAssemblyHash(package);
        return hash == null || trustStore.IsTrusted(package.Manifest.Id, hash);
    }

    public static void Trust(ModPackage package, ModTrustStore trustStore)
    {
        var hash = GetAssemblyHash(package);

        if (hash != null)
            trustStore.Trust(package.Manifest.Id, hash);
    }

    /// <summary>
    /// Applies the mod to the loaded base game data and initializes the mod code.
    /// </summary>
    /// <param name="baseVersion">Version of the base game like "1.20" (optional).</param>
    public static ModRuntime Load(ModPackage package, IGameData gameData, string? baseVersion)
    {
        var manifest = package.Manifest;

        if (!manifest.SupportsBaseGame(gameData.Advanced, baseVersion, out var reason))
            throw new InvalidOperationException(reason);

        var assetApplier = new ModAssetApplier(package.Source, gameData);
        assetApplier.Apply();

        var api = new ModApi(manifest, package.Source, gameData);
        ModBase? mod = null;

        if (manifest.HasCode)
        {
            var assembly = LoadAssembly(package);
            var modTypes = assembly.GetExportedTypes()
                .Where(type => !type.IsAbstract && typeof(ModBase).IsAssignableFrom(type))
                .ToList();

            if (modTypes.Count != 1)
                throw new InvalidDataException($"The mod assembly must contain exactly one public class deriving from {nameof(ModBase)} but {modTypes.Count} were found.");

            mod = (ModBase)Activator.CreateInstance(modTypes[0])!;
            mod.Api = api;
            mod.OnInitialize();
        }

        return new ModRuntime(package, api, mod, assetApplier.Warnings);
    }

    /// <summary>
    /// Each mod gets its own load context. Assemblies of the game (like Ambermoon.Mod)
    /// are taken from the default context so the mod uses the same types as the game.
    /// Further assemblies the mod depends on can be placed next to the mod assembly.
    /// </summary>
    sealed class ModLoadContext(IModSource source, string assemblyDirectory, string modId)
        : AssemblyLoadContext($"Mod:{modId}")
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Prefer the assemblies of the game.
            if (Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name) is Assembly loaded)
                return loaded;

            var dependencyPath = (assemblyDirectory.Length == 0 ? "" : assemblyDirectory + "/") + assemblyName.Name + ".dll";

            if (!source.Exists(dependencyPath))
                return null; // fallback to the default context

            using var stream = new MemoryStream(source.ReadAllBytes(dependencyPath));
            return LoadFromStream(stream);
        }
    }

    static Assembly LoadAssembly(ModPackage package)
    {
        var source = package.Source;
        var assemblyPath = package.Manifest.Assembly!;
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)?.Replace('\\', '/') ?? "";
        var context = new ModLoadContext(source, assemblyDirectory, package.Manifest.Id);

        using var assemblyStream = new MemoryStream(source.ReadAllBytes(assemblyPath));
        var symbolsPath = Path.ChangeExtension(assemblyPath, ".pdb");
        using var symbolStream = source.Exists(symbolsPath) ? new MemoryStream(source.ReadAllBytes(symbolsPath)) : null;

        return context.LoadFromStream(assemblyStream, symbolStream);
    }

    /// <summary>
    /// Wraps the savegame manager so the mod can store its own data with each savegame.
    /// </summary>
    public ISavegameManager WrapSavegameManager(ISavegameManager savegameManager, string saveDirectory)
        => new ModSavegameManager(savegameManager, saveDirectory, dispatcher);

    /// <summary>
    /// Connects the mod to the created game.
    /// </summary>
    public void AttachGame(GameCore game)
    {
        Api.AttachGame(game);
        game.ModHooks = dispatcher;
    }
}
