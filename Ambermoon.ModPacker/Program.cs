using System.IO.Compression;
using Ambermoon.Mod;
using Ambermoon.Mod.Packaging;

// Ambermoon mod packer
//
//   new <folder> <id> [name]       creates a new mod folder with a manifest and the asset folders
//   validate <folder|file.mod>     checks a mod
//   pack <folder> [output.mod]     checks a mod folder and packs it into a .mod file

static int Usage()
{
    Console.WriteLine("Ambermoon mod packer");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  AmbermoonModPacker new <folder> <id> [name]   Create a new mod folder");
    Console.WriteLine("  AmbermoonModPacker validate <folder|file.mod> Check a mod");
    Console.WriteLine("  AmbermoonModPacker pack <folder> [output.mod] Check and pack a mod folder");
    Console.WriteLine();
    Console.WriteLine("Copy the mod folder or the .mod file into the 'Mods' folder of Ambermoon.net.");
    return 1;
}

static bool Validate(IModSource source)
{
    var result = ModValidator.Validate(source);

    foreach (var warning in result.Warnings)
        Console.WriteLine($"Warning: {warning}");

    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error}");

    Console.WriteLine(result.Success ? "The mod is valid." : $"The mod has {result.Errors.Count} error(s).");

    return result.Success;
}

static int New(string folder, string id, string? name)
{
    if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
    {
        Console.WriteLine($"The folder '{folder}' is not empty.");
        return 1;
    }

    var manifest = new ModManifest
    {
        Id = id,
        Name = name ?? id,
        Description = "",
        Version = "1.0",
        Authors = [Environment.UserName],
        ReleaseDate = DateOnly.FromDateTime(DateTime.Today)
    };
    manifest.Validate();

    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, ModManifest.FileName), Newtonsoft.Json.JsonConvert.SerializeObject(manifest, new Newtonsoft.Json.JsonSerializerSettings(ModJson.Settings) { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore }));

    foreach (var assetFolder in new[] { "maps", "monsters", "monstergroups", "items", "partymembers", "npcs", "graphics", "music" })
        Directory.CreateDirectory(Path.Combine(folder, "assets", assetFolder));

    Console.WriteLine($"Created mod '{id}' in '{folder}'. See Modding.md for the asset formats.");
    return 0;
}

static int Pack(string folder, string? output)
{
    if (!Directory.Exists(folder))
    {
        Console.WriteLine($"The folder '{folder}' does not exist.");
        return 1;
    }

    using (var source = new FolderModSource(folder))
    {
        if (!Validate(source))
            return 1;
    }

    var manifest = ModManifest.Parse(File.ReadAllText(Path.Combine(folder, ModManifest.FileName)));
    output ??= Path.Combine(Path.GetDirectoryName(Path.GetFullPath(folder))!, manifest.Id + PackedModSource.Extension);

    if (File.Exists(output))
        File.Delete(output);

    using (var zip = ZipFile.Open(output, ZipArchiveMode.Create))
    {
        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(folder, file).Replace('\\', '/');

            // Skip hidden files and folders like .git
            if (relativePath.Split('/').Any(part => part.StartsWith('.')))
                continue;

            zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
        }
    }

    Console.WriteLine($"Packed '{manifest.Name}' {manifest.ParsedVersion} to '{output}'.");
    return 0;
}

if (args.Length < 2)
    return Usage();

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "new":
            return args.Length < 3 ? Usage() : New(args[1], args[2], args.Length > 3 ? string.Join(' ', args.Skip(3)) : null);
        case "validate":
        {
            using var package = Directory.Exists(args[1]) ? (IModSource)new FolderModSource(args[1]) : new PackedModSource(args[1]);
            return Validate(package) ? 0 : 1;
        }
        case "pack":
            return Pack(args[1], args.Length > 2 ? args[2] : null);
        default:
            return Usage();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
