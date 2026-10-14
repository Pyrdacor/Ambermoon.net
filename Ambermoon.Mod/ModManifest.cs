using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ambermoon.Mod;

/// <summary>
/// The base game a mod is built on.
/// </summary>
public enum ModBaseGame
{
    /// <summary>
    /// The mod works with any base game.
    /// </summary>
    Any,
    /// <summary>
    /// Original Ambermoon (1.19, 1.20, ...)
    /// </summary>
    Original,
    /// <summary>
    /// Ambermoon Advanced
    /// </summary>
    Advanced
}

public record ModVersion(int Major, int Minor)
{
    public override string ToString() => $"{Major}.{Minor:00}";

    public static ModVersion Parse(string version)
    {
        var parts = version.Trim().Split('.');

        if (parts.Length == 0 || parts.Length > 2 || !int.TryParse(parts[0], out int major))
            throw new FormatException($"Invalid mod version '{version}'. Use 'Major.Minor'.");

        int minor = 0;

        if (parts.Length == 2 && !int.TryParse(parts[1], out minor))
            throw new FormatException($"Invalid mod version '{version}'. Use 'Major.Minor'.");

        return new(major, minor);
    }
}

public class ModBaseRequirement
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ModBaseGame Game { get; set; } = ModBaseGame.Any;

    /// <summary>
    /// Optional minimum version of the base game like "1.20".
    /// </summary>
    public string? MinVersion { get; set; }
}

/// <summary>
/// The content of the "mod.json" file at the root of each mod.
/// </summary>
public class ModManifest
{
    public const string FileName = "mod.json";

    /// <summary>
    /// Version of the mod API. Increased on breaking changes.
    /// </summary>
    public const int CurrentApiVersion = 1;

    /// <summary>
    /// Unique id of the mod. Only letters, digits, '-', '_' and '.' are allowed.
    /// It is used for the savegame folder and the trust store.
    /// </summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string[] Authors { get; set; } = [];
    public DateOnly? ReleaseDate { get; set; }
    public int ApiVersion { get; set; } = CurrentApiVersion;
    public ModBaseRequirement Base { get; set; } = new();
    /// <summary>
    /// Optional file name of the mod assembly (relative to the mod root).
    /// If not given, the mod is an asset-only mod.
    /// </summary>
    public string? Assembly { get; set; }

    [JsonIgnore]
    public bool HasCode => !string.IsNullOrWhiteSpace(Assembly);

    [JsonIgnore]
    public ModVersion ParsedVersion => ModVersion.Parse(Version);

    public static ModManifest Parse(string json)
    {
        var manifest = JsonConvert.DeserializeObject<ModManifest>(json)
            ?? throw new InvalidDataException("Empty mod manifest.");

        manifest.Validate();

        return manifest;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidDataException("The mod manifest needs an 'Id'.");

        if (Id.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-' && ch != '_' && ch != '.'))
            throw new InvalidDataException($"Invalid mod id '{Id}'. Only letters, digits, '-', '_' and '.' are allowed.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidDataException("The mod manifest needs a 'Name'.");

        _ = ParsedVersion;

        if (ApiVersion > CurrentApiVersion)
            throw new InvalidDataException($"The mod needs mod API version {ApiVersion} but only version {CurrentApiVersion} is supported. Please update Ambermoon.net.");

        if (Assembly != null && (Path.IsPathRooted(Assembly) || Assembly.Contains("..")))
            throw new InvalidDataException("The mod assembly must be a relative path inside the mod.");
    }

    /// <summary>
    /// Checks if the mod can run on the given base game.
    /// </summary>
    public bool SupportsBaseGame(bool advanced, string? version, out string reason)
    {
        reason = "";

        if (Base.Game == ModBaseGame.Original && advanced)
        {
            reason = "The mod needs the original Ambermoon as base game.";
            return false;
        }

        if (Base.Game == ModBaseGame.Advanced && !advanced)
        {
            reason = "The mod needs Ambermoon Advanced as base game.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Base.MinVersion) && !string.IsNullOrWhiteSpace(version) &&
            TryParseVersion(version, out var actual) && TryParseVersion(Base.MinVersion, out var required) &&
            actual < required)
        {
            reason = $"The mod needs at least version {Base.MinVersion} of the base game.";
            return false;
        }

        return true;
    }

    static bool TryParseVersion(string version, out System.Version result)
    {
        // Versions like "1.20" or "v1.20 (Advanced)"
        var digits = new string(version.SkipWhile(ch => !char.IsAsciiDigit(ch)).TakeWhile(ch => char.IsAsciiDigit(ch) || ch == '.').ToArray());

        return System.Version.TryParse(digits, out result!);
    }
}
