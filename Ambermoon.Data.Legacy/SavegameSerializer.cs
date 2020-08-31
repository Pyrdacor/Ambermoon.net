using System;

namespace Ambermoon.Data.Legacy
{
    public class SavegameSerializer : ISavegameSerializer
    {
        void ReadSaveData(Savegame savegame, IDataReader dataReader)
        {
            dataReader.Position += 10;
            savegame.CurrentMapIndex = dataReader.ReadWord();
            savegame.CurrentMapX = dataReader.ReadWord();
            savegame.CurrentMapY = dataReader.ReadWord();
            savegame.CharacterDirection = (CharacterDirection)dataReader.ReadWord();

            dataReader.Position = 42;

            dataReader.ReadWord(); // Number of party members. We don't really need it.
            savegame.ActivePartyMemberSlot = dataReader.ReadWord() - 1; // it is stored 1-based

            for (int i = 0; i < 6; ++i)
                savegame.CurrentPartyMemberIndices[i] = dataReader.ReadWord();

            // TODO: load other data from Party_data.sav
        }

        public void Read(Savegame savegame, SavegameFiles files)
        {
            var partyMemberReader = new Characters.PartyMemberReader();
            var chestReader = new ChestReader();
            var merchantReader = new MerchantReader();
            // TODO automap reader

            savegame.PartyMembers.Clear();
            savegame.Chests.Clear();
            savegame.Merchants.Clear();
            // TODO automaps

            foreach (var partyMemberDataReader in files.PartyMemberDataReaders.Files)
                savegame.PartyMembers.Add(PartyMember.Load(partyMemberReader, partyMemberDataReader.Value));
            foreach (var chestDataReader in files.ChestDataReaders.Files)
                savegame.Chests.Add(Chest.Load(chestReader, chestDataReader.Value));
            foreach (var merchantDataReader in files.MerchantDataReaders.Files)
                savegame.Merchants.Add(Merchant.Load(merchantReader, merchantDataReader.Value));
            // TODO automaps

            ReadSaveData(savegame, files.SaveDataReader);
        }

        public void Write(Savegame savegame, SavegameFiles files)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads a savegame from a legacy game data save slot.
        /// </summary>
        /// <param name="saveSlot">0 to 10 where 0 is the default save game.</param>
        public static Savegame Load(ISavegameSerializer savegameSerializer, IGameData gameData, int saveSlot)
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

        public static Savegame LoadInitial(ISavegameSerializer savegameSerializer, IGameData gameData)
        {
            var savegame = new Savegame();
            var savegameFiles = new SavegameFiles
            {
                SaveDataReader = gameData.Files["Initial/Party_data.sav"].Files[1],
                PartyMemberDataReaders = gameData.Files["Initial/Party_char.amb"],
                ChestDataReaders = gameData.Files["Initial/Chest_data.amb"],
                MerchantDataReaders = gameData.Files["Initial/Merchant_data.amb"],
                AutomapDataReaders = gameData.Files["Initial/Automap.amb"]
            };

            savegameSerializer.Read(savegame, savegameFiles);

            return savegame;
        }
    }
}
