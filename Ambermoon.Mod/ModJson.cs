using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ambermoon.Mod;

/// <summary>
/// JSON settings used for all mod asset files.
/// Enums can be given by name ("Map3D") or flags as comma separated names ("Indoor, CanRest").
/// </summary>
public static class ModJson
{
    public static JsonSerializerSettings Settings { get; } = new()
    {
        Converters = { new StringEnumConverter() },
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Error,
        Formatting = Formatting.Indented
    };
}
