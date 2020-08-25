namespace Ambermoon.Data.Legacy
{
    public class SavegameManager : ISavegameManager
    {
        public Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot)
        {
            var savegame = new Savegame();
            var savegameFiles = new SavegameFiles
            {
                SaveDataReader = gameData.Files[$"Save.{saveSlot:00}/Party_data.sav"].Files[1],
                PartyMemberDataReaders = gameData.Files[$"Save.{saveSlot:00}/Party_char.amb"],
                ChestDataReaders = gameData.Files[$"Save.{saveSlot:00}/Chest_data.amb"],
                MerchantDataReaders = gameData.Files[$"Save.{saveSlot:00}/Merchant_data.amb"],
                AutomapDataReaders = gameData.Files[$"Save.{saveSlot:00}/Automap.amb"]
            };

            savegameSerializer.Read(savegame, savegameFiles);

            return savegame;
        }

        public Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer)
        {
            var savegame = new Savegame();
            SavegameFiles savegameFiles;

            try
            {
                savegameFiles = new SavegameFiles
                {
                    SaveDataReader = gameData.Files["Initial/Party_data.sav"].Files[1],
                    PartyMemberDataReaders = gameData.Files["Initial/Party_char.amb"],
                    ChestDataReaders = gameData.Files["Initial/Chest_data.amb"],
                    MerchantDataReaders = gameData.Files["Initial/Merchant_data.amb"],
                    AutomapDataReaders = gameData.Files["Initial/Automap.amb"]
                };
            }
            catch
            {
                savegameFiles = new SavegameFiles
                {
                    SaveDataReader = gameData.Files["Party_data.sav"].Files[1],
                    PartyMemberDataReaders = gameData.Files["Party_char.amb"],
                    ChestDataReaders = gameData.Files["Chest_data.amb"],
                    MerchantDataReaders = gameData.Files["Merchant_data.amb"],
                    AutomapDataReaders = gameData.Files["Automap.amb"]
                };
            }

            savegameSerializer.Read(savegame, savegameFiles);
            return savegame;
        }
    }
}
