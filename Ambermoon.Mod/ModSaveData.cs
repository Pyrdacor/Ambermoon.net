using System.Globalization;
using Newtonsoft.Json;

namespace Ambermoon.Mod;

/// <summary>
/// Key/value storage which is saved alongside each savegame.
/// </summary>
public class ModSaveData
{
    readonly Dictionary<string, string> values;

    public ModSaveData()
        : this([])
    {
    }

    ModSaveData(Dictionary<string, string> values)
    {
        this.values = values;
    }

    public bool IsEmpty => values.Count == 0;

    public IReadOnlyCollection<string> Keys => values.Keys;

    public bool Contains(string key) => values.ContainsKey(key);

    public void Remove(string key) => values.Remove(key);

    public void Clear() => values.Clear();

    public void Set(string key, string value) => values[key] = value;

    public void Set(string key, long value) => values[key] = value.ToString(CultureInfo.InvariantCulture);

    public void Set(string key, bool value) => values[key] = value ? "1" : "0";

    /// <summary>
    /// Stores any object as JSON.
    /// </summary>
    public void SetObject<T>(string key, T value) => values[key] = JsonConvert.SerializeObject(value);

    public string? GetString(string key) => values.TryGetValue(key, out var value) ? value : null;

    public long GetLong(string key, long defaultValue = 0)
        => values.TryGetValue(key, out var value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : defaultValue;

    public int GetInt(string key, int defaultValue = 0) => (int)GetLong(key, defaultValue);

    public bool GetBool(string key, bool defaultValue = false) => values.TryGetValue(key, out var value) ? value == "1" : defaultValue;

    public T? GetObject<T>(string key) => values.TryGetValue(key, out var value) ? JsonConvert.DeserializeObject<T>(value) : default;

    public string ToJson() => JsonConvert.SerializeObject(values, Formatting.Indented);

    public static ModSaveData FromJson(string json)
        => new(JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? []);
}
