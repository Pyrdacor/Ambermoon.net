using Ambermoon.Data.Serialization;
using System;

namespace Ambermoon.Data
{
    public interface ISavegameManager
    {
        Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer);
        Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, int totalSavegames);
        bool HasCrashSavegame();
        bool RemoveCrashedSavegame();
        void SaveCrashedGame(ISavegameSerializer savegameSerializer, Savegame savegame);
        void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame);
        string[] GetSavegameNames(IGameData gameData, out int current, int totalSavegames);
        void WriteSavegameName(IGameData gameData, int slot, ref string name, string externalSavesPath);
        void SetActiveSavegame(IGameData gameData, int slot);
    }
}
