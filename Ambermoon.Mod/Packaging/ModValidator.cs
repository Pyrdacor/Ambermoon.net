using Ambermoon.Data;
using Ambermoon.Mod.Assets;
using Newtonsoft.Json.Linq;

namespace Ambermoon.Mod.Packaging;

/// <summary>
/// Checks a mod without the base game data: manifest, JSON syntax,
/// map definitions, images and file names. References to the base game
/// (like monster or item indices) can only be checked when the mod is loaded.
/// </summary>
public static class ModValidator
{
    public record Result(List<string> Errors, List<string> Warnings)
    {
        public bool Success => Errors.Count == 0;
    }

    static readonly string[] IndexedAssetFolders = ["maps", "monsters", "monstergroups", "items", "partymembers", "npcs"];

    public static Result Validate(IModSource source)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ModManifest? manifest = null;

        try
        {
            if (!source.Exists(ModManifest.FileName))
                errors.Add($"{ModManifest.FileName} is missing.");
            else
                manifest = ModManifest.Parse(source.ReadAllText(ModManifest.FileName));
        }
        catch (Exception ex)
        {
            errors.Add($"{ModManifest.FileName}: {ex.Message}");
        }

        if (manifest?.HasCode == true && !source.Exists(manifest.Assembly!))
            errors.Add($"The mod assembly '{manifest.Assembly}' is missing.");

        const string assets = ModAssetApplier.AssetFolder;
        var knownFolders = IndexedAssetFolders.Concat(["graphics", "music"]).ToHashSet();

        foreach (var file in source.ListFiles(assets))
        {
            var parts = file.Split('/');

            if (parts.Length < 3 || !knownFolders.Contains(parts[1]))
            {
                warnings.Add($"'{file}' is not used (unknown asset folder).");
                continue;
            }

            string folder = parts[1];

            try
            {
                if (IndexedAssetFolders.Contains(folder))
                {
                    if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add($"'{file}' is not used (only .json files).");
                        continue;
                    }

                    if (!uint.TryParse(Path.GetFileNameWithoutExtension(file), out uint index) || index == 0)
                    {
                        errors.Add($"'{file}': the file name must be a 1-based index.");
                        continue;
                    }

                    var json = JObject.Parse(source.ReadAllText(file));

                    if (folder == "maps" && json.ContainsKey(nameof(MapDefinition.Grid)))
                    {
                        if (index > 767)
                            errors.Add($"'{file}': map indices must be between 1 and 767.");

                        json.ToObject<MapDefinition>(Newtonsoft.Json.JsonSerializer.Create(ModJson.Settings))!.CreateMap(index);
                    }
                }
                else if (folder == "graphics")
                {
                    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (parts.Length != 4 || !Enum.TryParse<GraphicType>(parts[2], true, out _) ||
                        !int.TryParse(Path.GetFileNameWithoutExtension(parts[3]), out _) ||
                        !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add($"'{file}' is not used (expected graphics/{{GraphicType}}/{{index}}.png).");
                        continue;
                    }

                    PngDecoder.Decode(source.ReadAllBytes(file));
                }
                else if (folder == "music")
                {
                    var name = Path.GetFileNameWithoutExtension(file);

                    if (!file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || name.Length == 0 || !char.IsAsciiDigit(name[0]))
                        warnings.Add($"'{file}' is not used (expected music/{{song number}}.mp3).");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"'{file}': {ex.Message}");
            }
        }

        return new(errors, warnings);
    }
}
