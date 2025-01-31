using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Legacy;

namespace AmbermoonAndroid;

internal class RemakeSavegameManager(string path, Configuration configuration) : SavegameManager(path), IAdditionalSaveSlotProvider
{
    readonly Configuration configuration = configuration;
    readonly string gameVersionName = Path.GetFileName(path);
    readonly string savesPath = Path.Combine(path, "Saves");
    readonly string savegameNamesPath = Path.Combine(path, "Saves.cfg");
    AdditionalSavegameSlots additionalSavegameSlots = null;

    AdditionalSavegameSlots AdditionalSavegameSlots
    {
        get
        {
            additionalSavegameSlots ??= GetOrCreateAdditionalSavegameNames(gameVersionName);

            return additionalSavegameSlots;
        }
    }

    public int ContinueSavegameSlot => AdditionalSavegameSlots.ContinueSavegameSlot;

    public AdditionalSavegameSlots GetOrCreateAdditionalSavegameNames(string gameVersionName)
    {
        // Note: It is no longer necessary to provide the gameVersionName here, but for
        // compatibility reasons, we keep it this way.

        try
        {
            if (!File.Exists(savegameNamesPath))
            {
                additionalSavegameSlots = configuration.GetOrCreateCurrentAdditionalSavegameSlots(gameVersionName);
                additionalSavegameSlots.Save(savegameNamesPath);
            }
            else
            {
                additionalSavegameSlots = AdditionalSavegameSlots.Load(savegameNamesPath);
            }
        }
        catch
        {
            additionalSavegameSlots = configuration.GetOrCreateCurrentAdditionalSavegameSlots(gameVersionName);
        }

        return additionalSavegameSlots;
    }

    public override string[] GetSavegameNames(IGameData gameData, out int current, int totalSavegames)
    {
        var additionalSavegameSlots = AdditionalSavegameSlots;
        DateTime? lastLegacyFileUpdate = File.Exists(savesPath) ? new FileInfo(savesPath).LastWriteTimeUtc : null;
        var lastSavesSync = additionalSavegameSlots.LastSavesSync;
        string[] savegameNames = new string[Game.NumBaseSavegameSlots];

        if (lastLegacyFileUpdate != null && (lastSavesSync == null || lastSavesSync.Value < lastLegacyFileUpdate.Value))
        {
            // Legacy savegame names are newer, use them.
            var legacyNames = base.GetSavegameNames(gameData, out current, totalSavegames);

            for (int i = 0; i < Game.NumBaseSavegameSlots; i++)
            {
                if (string.IsNullOrWhiteSpace(legacyNames[i]))
                {
                    savegameNames[i] = additionalSavegameSlots.BaseNames[i] ?? string.Empty;
                }
                else
                {
                    savegameNames[i] = legacyNames[i];
                }
            }

            if (current == 0)
                current = additionalSavegameSlots.ContinueSavegameSlot;
        }
        else
        {
            current = additionalSavegameSlots.ContinueSavegameSlot;

            if (current == 0)
                base.GetSavegameNames(gameData, out current, totalSavegames);

            for (int i = 0; i < Game.NumBaseSavegameSlots; i++)
            {
                savegameNames[i] = additionalSavegameSlots.BaseNames[i] ?? string.Empty;
            }
        }

        if (current < 0)
            current = 0;
        else if (current > 10)
            current = 10;

        if (current != 0 && string.IsNullOrWhiteSpace(savegameNames[current]))
            current = 0;

        return savegameNames;
    }

    public void RequestSave()
    {
        try
        {
            additionalSavegameSlots?.Save(savegameNamesPath);
        }
        catch
        {
            // Fallback to old system where savenames are stored in config

            var configAdditionalSavegameSlots = configuration.GetOrCreateCurrentAdditionalSavegameSlots(additionalSavegameSlots.GameVersionName);

            configAdditionalSavegameSlots.ContinueSavegameSlot = additionalSavegameSlots.ContinueSavegameSlot;
            configAdditionalSavegameSlots.Names = additionalSavegameSlots.Names;

            configuration.RequestSave();
        }
    }

    public override void WriteSavegameName(IGameData gameData, int slot, ref string name, string externalSavesPath)
    {
        base.WriteSavegameName(gameData, slot, ref name, externalSavesPath);

        var additionalSavegameSlots = AdditionalSavegameSlots;

        if (slot >= 1 && slot <= 10)
        {
            var lastSavesSyncBackup = additionalSavegameSlots.LastSavesSync;

            additionalSavegameSlots.BaseNames[slot] = name;
            additionalSavegameSlots.LastSavesSync = DateTime.UtcNow;

            try
            {
                additionalSavegameSlots?.Save(savegameNamesPath);
            }
            catch
            {
                additionalSavegameSlots.LastSavesSync = lastSavesSyncBackup;
            }
        }
    }
}

