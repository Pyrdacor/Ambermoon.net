using Ambermoon.Data;
using Newtonsoft.Json;

namespace Ambermoon.Game;

public interface IAdditionalSaveSlotProvider
{
    AdditionalSavegameSlots GetOrCreateAdditionalSavegameNames(string gameVersionName);
    void RequestSave(ISavegameManager savegameManager, IGameData gameData);
}

public class AdditionalSavegameSlots
{
    public required string GameVersionName { get; set; }
    public string[] BaseNames { get; set; } = new string[Game.NumBaseSavegameSlots];
    public string[] Names { get; set; } = new string[Game.NumAdditionalSavegameSlots];
    public int ContinueSavegameSlot { get; set; } = 0;
    public DateTime? LastSavesSync { get; set; } = null;

    public static AdditionalSavegameSlots Load(string path)
    {
        return JsonConvert.DeserializeObject<AdditionalSavegameSlots>(File.ReadAllText(path))!;
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}

public interface IConfiguration : ICoreConfiguration
{
    bool UseDataPath { get; set; }
    string DataPath { get; set; }
    SaveOption SaveOption { get; set; }
    [Obsolete("Use AdditionalSavegameSlots instead.")]
    string[] AdditionalSavegameNames { get; set; }
    [Obsolete("Use AdditionalSavegameSlots instead.")]
    int? ContinueSavegameSlot { get; set; }
    AdditionalSavegameSlots[] AdditionalSavegameSlots { get; set; }

    AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots(string gameVersionName);
}