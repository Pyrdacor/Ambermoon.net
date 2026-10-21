using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Ambermoon.Mod;

/// <summary>
/// Default values for game options which a mod provides (the "Options" of the mod manifest).
///
/// The defaults are stored in a mod config file the first time the mod is used. While the mod
/// is active, an option is read from this file and only if it isn't there from the normal
/// configuration. When the player changes the option, the new value is written to the normal
/// configuration and the option is removed from the mod config file. So a mod only provides
/// defaults and never enforces an option.
/// </summary>
public class ModOptions
{
    /// <summary>
    /// The options a mod can provide defaults for (name and type). Options like the resolution,
    /// window mode, music, volume, language or graphic filters stay under the control of the player.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Type> AllowedOptions = new Dictionary<string, Type>
    {
        { nameof(ICoreConfiguration.BattleSpeed), typeof(int) },
        { nameof(ICoreConfiguration.AutoBattleRounds), typeof(int) },
        { nameof(ICoreConfiguration.AutoDerune), typeof(bool) },
        { nameof(ICoreConfiguration.ShowButtonTooltips), typeof(bool) },
        { nameof(ICoreConfiguration.ShowPlayerStatsTooltips), typeof(bool) },
        { nameof(ICoreConfiguration.ShowFloor), typeof(bool) },
        { nameof(ICoreConfiguration.ShowCeiling), typeof(bool) },
        { nameof(ICoreConfiguration.ShowFog), typeof(bool) },
        { nameof(ICoreConfiguration.ShowSaveLoadMessage), typeof(bool) },
        { nameof(ICoreConfiguration.Movement3D), typeof(Movement3D) },
        { nameof(ICoreConfiguration.TurnWithArrowKeys), typeof(bool) },
    };

    static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings { Converters = { new StringEnumConverter() } });

    class StoredOptions
    {
        /// <summary>
        /// All defaults which were ever added. Defaults which were added by a newer
        /// version of the mod are added once, but removed ones are never added again.
        /// </summary>
        public List<string> Applied { get; set; } = [];
        public Dictionary<string, JToken> Options { get; set; } = [];
    }

    readonly string? file;
    readonly StoredOptions stored;
    readonly Dictionary<string, object> values = [];

    ModOptions(string? file, StoredOptions stored)
    {
        this.file = file;
        this.stored = stored;

        foreach (var option in stored.Options)
        {
            if (AllowedOptions.TryGetValue(option.Key, out var type) && TryConvert(option.Value, type, out var value))
                values[option.Key] = value;
        }
    }

    /// <summary>
    /// The current mod defaults (the ones the player hasn't changed yet).
    /// </summary>
    public IReadOnlyDictionary<string, object> Values => values;

    static bool TryConvert(JToken token, Type type, out object value)
    {
        bool matchingToken = token.Type switch
        {
            JTokenType.Boolean => type == typeof(bool),
            JTokenType.Integer => type == typeof(int),
            JTokenType.String => type.IsEnum,
            _ => false
        };

        if (!matchingToken)
        {
            value = null!;
            return false;
        }

        try
        {
            value = token.ToObject(type, Serializer)!;
            return value != null && (!type.IsEnum || Enum.IsDefined(type, value));
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or FormatException or InvalidCastException)
        {
            value = null!;
            return false;
        }
    }

    /// <summary>
    /// Checks the options of a mod manifest.
    /// </summary>
    public static void Validate(IReadOnlyDictionary<string, JToken> options)
    {
        foreach (var option in options)
        {
            if (!AllowedOptions.TryGetValue(option.Key, out var type))
            {
                throw new InvalidDataException($"The option '{option.Key}' can't be set by mods. Allowed options are: " +
                    string.Join(", ", AllowedOptions.Keys) + ".");
            }

            if (!TryConvert(option.Value, type, out _))
                throw new InvalidDataException($"Invalid value '{option.Value}' for the option '{option.Key}' ({type.Name} expected).");
        }
    }

    /// <summary>
    /// Loads the mod config file and adds the defaults of the manifest which were not added before.
    /// </summary>
    /// <param name="file">Mod config file. If null, nothing is stored (for tests).</param>
    public static ModOptions Load(ModManifest manifest, string? file)
    {
        StoredOptions? stored = null;

        try
        {
            if (file != null && File.Exists(file))
                stored = JsonConvert.DeserializeObject<StoredOptions>(File.ReadAllText(file));
        }
        catch (JsonException)
        {
            // A broken file is replaced.
        }

        stored ??= new StoredOptions();
        bool changed = false;

        foreach (var option in manifest.Options ?? [])
        {
            if (stored.Applied.Contains(option.Key))
                continue;

            stored.Applied.Add(option.Key);
            stored.Options[option.Key] = option.Value;
            changed = true;
        }

        var options = new ModOptions(file, stored);

        if (changed)
            options.Save();

        return options;
    }

    public bool TryGet<T>(string name, out T value)
    {
        if (values.TryGetValue(name, out var stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Removes the default of an option (the player changed it).
    /// </summary>
    public void Remove(string name)
    {
        if (!values.Remove(name))
            return;

        stored.Options.Remove(name);
        Save();
    }

    void Save()
    {
        if (file == null)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, JsonConvert.SerializeObject(stored, Formatting.Indented));
        }
        catch (IOException)
        {
            // Not critical: the defaults are used again next time.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
