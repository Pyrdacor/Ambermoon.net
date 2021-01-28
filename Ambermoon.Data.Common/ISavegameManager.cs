using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public interface ISavegameManager
    {
        Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer);
        Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot);
        void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame);
        string[] GetSavegameNames(IGameData gameData, out int current);
        void WriteSavegameName(IGameData gameData, int slot, ref string name);
    }
}
