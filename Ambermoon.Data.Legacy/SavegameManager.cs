namespace Ambermoon.Data.Legacy
{
    public class SavegameManager : ISavegameManager
    {
        string[] savegameNames = null;
        int current = 0;

        public string[] GetSavegameNames(IGameData gameData, out int current)
        {
            current = this.current;

            if (savegameNames != null)
                return savegameNames;

            var file = gameData.Files["Saves"].Files[1];
            this.current = current = file.ReadWord();
            savegameNames = new string[10];
            int position = file.Position;

            for (int i = 0; i < 10; ++i)
            {
                savegameNames[i] = file.ReadNullTerminatedString();

                if (i < 9)
                {
                    position += 39;
                    file.Position = position;
                }
            }

            return savegameNames;
        }

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
