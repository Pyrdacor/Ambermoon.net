using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor;
using Ambermoon.Data.Serialization;
using Newtonsoft.Json;

namespace Ambermoon.Mod;

public class DefaultModSavegameManager : ISavegameManager
{
    public record SavegameConfig
    {
        public List<string> Names { get; set; } = [];
        public int Current { get; set; } = 0;
    }

    readonly string savegamePath;
    readonly string savegameConfigPath;
    readonly SavegameConfig config;

    public DefaultModSavegameManager(ModGameData modGameData)
    {
        savegamePath = Path.Combine(modGameData.ModDirectory, "Savegames");

        try
        {
            Directory.CreateDirectory(savegamePath);
        }
        catch
        {
            savegamePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ambermoon", "Mods", Path.GetFileName(modGameData.ModDirectory), "Savegames");

            Directory.CreateDirectory(savegamePath);
        }

        savegameConfigPath = Path.Combine(savegamePath, "saves.json");

        if (!File.Exists(savegameConfigPath))
        {
            config = new();
            SaveConfig();
        }
        else
        {
            config = JsonConvert.DeserializeObject<SavegameConfig>(File.ReadAllText(savegameConfigPath))!;
        }
    }

    public string[] GetSavegameNames(IGameData gameData, out int current, int totalSavegames)
    {
        current = Math.Max(0, Math.Min(config.Current, totalSavegames));
        var savegameNames = config.Names;

        if (totalSavegames > savegameNames.Count)
            return savegameNames.Concat(Enumerable.Repeat("", totalSavegames - savegameNames.Count)).ToArray();

        return savegameNames.Take(totalSavegames).ToArray();
    }

    public bool HasCrashSavegame()
    {
        return File.Exists(Path.Combine(savegamePath, $"Save99.sav"));
    }

    public Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, int totalSavegames)
    {
        if (saveSlot == 0)
            return (gameData as ModGameData)!.LoadInitial();

        string file = Path.Combine(savegamePath, $"Save{saveSlot:00}.sav");

        if (!File.Exists(file))
            throw new FileNotFoundException($"File \"{file}\" was not found.");

        return GameData.LoadGame(new DataReader(File.ReadAllBytes(file)), (gameData as GameData)!);
    }

    public Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer)
    {
        return Load(gameData, savegameSerializer, 0, 1);
    }

    public bool RemoveCrashedSavegame()
    {
        try
        {
            File.Delete(Path.Combine(savegamePath, $"Save99.sav"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame)
    {
        if (saveSlot <= 0)
            throw new InvalidOperationException("You cannot save in slot 0, because it is the savegame for new games.");

        var writer = new DataWriter();

        GameData.SaveGame(writer, savegame);

        File.WriteAllBytes(Path.Combine(savegamePath, $"Save{saveSlot:00}.sav"), writer.ToArray());
    }

    public void SaveCrashedGame(ISavegameSerializer savegameSerializer, Savegame savegame)
    {
        Save(null!, savegameSerializer, 99, "", savegame);
    }

    public void SetActiveSavegame(IGameData gameData, int slot)
    {
        config.Current = slot;

        SaveConfig();
    }

    public void WriteSavegameName(IGameData gameData, int slot, ref string name, string externalSavesPath)
    {
        if (slot <= 0)
            throw new InvalidOperationException("You cannot write the savegame name for slot 0, because it is the savegame for new games.");

        slot--;

        if (config.Names.Count == slot)
            config.Names.Add(name);
        else if (config.Names.Count > slot)
            config.Names[slot] = name;
        else
        {
            config.Names.AddRange(Enumerable.Repeat("", slot - config.Names.Count));
            config.Names.Add(name);
        }

        SaveConfig();
    }

    private void SaveConfig()
    {
        File.WriteAllText(savegameConfigPath, JsonConvert.SerializeObject(config));
    }
}
