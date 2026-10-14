using Ambermoon.Data;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Mod;

/// <summary>
/// Wraps the savegame manager of the base game and stores the
/// save data of the mod next to each savegame ("ModData.{slot}.json").
/// </summary>
internal class ModSavegameManager(ISavegameManager baseManager, string saveDirectory, ModHookDispatcher dispatcher) : ISavegameManager
{
    readonly ISavegameManager baseManager = baseManager;
    readonly string saveDirectory = saveDirectory;
    readonly ModHookDispatcher dispatcher = dispatcher;
    const int CrashSlot = 99;

    string ModDataPath(int slot) => Path.Combine(saveDirectory, $"ModData.{slot:00}.json");

    void SaveModData(int slot)
    {
        var data = new ModSaveData();
        dispatcher.Saving(data);
        Directory.CreateDirectory(saveDirectory);
        File.WriteAllText(ModDataPath(slot), data.ToJson());
    }

    void LoadModData(int slot)
    {
        var path = ModDataPath(slot);
        var data = File.Exists(path) ? ModSaveData.FromJson(File.ReadAllText(path)) : new ModSaveData();
        dispatcher.Loading(data);
    }

    // Note: The initial savegame is also loaded internally by the game (e.g. to get
    // the initial chest contents), so the mod data is not touched here. For new games
    // mods are notified by ModBase.OnGameStarting with newGame = true.
    public Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer)
        => baseManager.LoadInitial(gameData, savegameSerializer);

    public Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, int totalSavegames)
    {
        var savegame = baseManager.Load(gameData, savegameSerializer, saveSlot, totalSavegames);

        if (savegame != null)
            LoadModData(saveSlot);

        return savegame!;
    }

    public void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame)
    {
        baseManager.Save(gameData, savegameSerializer, saveSlot, name, savegame);
        SaveModData(saveSlot);
    }

    public void SaveCrashedGame(ISavegameSerializer savegameSerializer, Savegame savegame)
    {
        baseManager.SaveCrashedGame(savegameSerializer, savegame);

        try
        {
            SaveModData(CrashSlot);
        }
        catch
        {
            // ignore
        }
    }

    public bool HasCrashSavegame() => baseManager.HasCrashSavegame();

    public bool RemoveCrashedSavegame()
    {
        try
        {
            File.Delete(ModDataPath(CrashSlot));
        }
        catch
        {
            // ignore
        }

        return baseManager.RemoveCrashedSavegame();
    }

    public string[] GetSavegameNames(IGameData gameData, out int current, int totalSavegames)
        => baseManager.GetSavegameNames(gameData, out current, totalSavegames);

    public void WriteSavegameName(IGameData gameData, int slot, ref string name, string externalSavesPath)
        => baseManager.WriteSavegameName(gameData, slot, ref name, externalSavesPath);

    public void SetActiveSavegame(IGameData gameData, int slot) => baseManager.SetActiveSavegame(gameData, slot);
}
