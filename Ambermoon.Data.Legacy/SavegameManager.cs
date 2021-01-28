using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class SavegameManager : ISavegameManager
    {
        readonly string path;
        readonly string savesPath;
        bool transferredFolderSaves = false;

        public SavegameManager(string path)
        {
            this.path = path;
            savesPath = Path.Combine(path, "Saves");
        }

        public string[] GetSavegameNames(IGameData gameData, out int current)
        {
            current = 0;

            if (File.Exists(savesPath))
            {
                return SavegameSerializer.GetSavegameNames(new DataReader(File.ReadAllBytes(savesPath)), ref current);
            }
            else if (!gameData.Files.ContainsKey("Saves"))
            {
                return Enumerable.Repeat("", 10).ToArray();
            }
            else
            {
                return SavegameSerializer.GetSavegameNames(gameData.Files["Saves"].Files[1], ref current);
            }            
        }

        public void WriteSavegameName(IGameData gameData, int slot, ref string name)
        {
            SavegameSerializer.WriteSavegameName(gameData, slot, ref name);
        }

        public Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot)
        {
            if (!transferredFolderSaves && File.Exists(savesPath))
            {
                transferredFolderSaves = true;
                var folderSaveData = new GameData(GameData.LoadPreference.ForceExtracted, null, false);

                try
                {
                    folderSaveData.Load(path);

                    void TransferFile(string name)
                    {
                        gameData.Files[name] = folderSaveData.Files[name];
                    }

                    for (int i = 1; i <= 10; ++i)
                    {
                        TransferFile($"Save.{saveSlot:00}/Party_data.sav");
                        TransferFile($"Save.{saveSlot:00}/Party_char.amb");
                        TransferFile($"Save.{saveSlot:00}/Chest_data.amb");
                        TransferFile($"Save.{saveSlot:00}/Merchant_data.amb");
                        TransferFile($"Save.{saveSlot:00}/Automap.amb");
                    }

                    TransferFile("Saves");
                }
                catch
                {
                    // ignore
                }
            }

            var savegame = new Savegame();
            SavegameInputFiles savegameFiles;
            try
            {
                savegameFiles = new SavegameInputFiles
                {
                    SaveDataReader = gameData.Files[$"Save.{saveSlot:00}/Party_data.sav"].Files[1],
                    PartyMemberDataReaders = gameData.Files[$"Save.{saveSlot:00}/Party_char.amb"],
                    ChestDataReaders = gameData.Files[$"Save.{saveSlot:00}/Chest_data.amb"],
                    MerchantDataReaders = gameData.Files[$"Save.{saveSlot:00}/Merchant_data.amb"],
                    AutomapDataReaders = gameData.Files[$"Save.{saveSlot:00}/Automap.amb"]
                };
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

            savegameSerializer.Read(savegame, savegameFiles, gameData.Files["Party_texts.amb"]);

            return savegame;
        }

        public Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer)
        {
            var savegame = new Savegame();
            SavegameInputFiles savegameFiles;
            IFileContainer partyTextContainer;

            try
            {
                savegameFiles = new SavegameInputFiles
                {
                    SaveDataReader = gameData.Files["Initial/Party_data.sav"].Files[1],
                    PartyMemberDataReaders = gameData.Files["Initial/Party_char.amb"],
                    ChestDataReaders = gameData.Files["Initial/Chest_data.amb"],
                    MerchantDataReaders = gameData.Files["Initial/Merchant_data.amb"],
                    AutomapDataReaders = gameData.Files["Initial/Automap.amb"]
                };
                partyTextContainer = gameData.Files["Party_texts.amb"];
            }
            catch
            {
                savegameFiles = new SavegameInputFiles
                {
                    SaveDataReader = gameData.Files["Party_data.sav"].Files[1],
                    PartyMemberDataReaders = gameData.Files["Party_char.amb"],
                    ChestDataReaders = gameData.Files["Chest_data.amb"],
                    MerchantDataReaders = gameData.Files["Merchant_data.amb"],
                    AutomapDataReaders = gameData.Files["Automap.amb"]
                };
                partyTextContainer = gameData.Files["Party_texts.amb"];
            }

            savegameSerializer.Read(savegame, savegameFiles, partyTextContainer);
            return savegame;
        }

        public void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame)
        {
            var savegameFiles = savegameSerializer.Write(savegame);
            WriteSavegameName(gameData, saveSlot, ref name);
            SaveToGameData(gameData, savegameFiles, saveSlot);
            SaveToPath(path, savegameFiles, saveSlot, gameData.Files["Saves"]);
        }

        void SaveToGameData(IGameData gameData, SavegameOutputFiles savegameFiles, int saveSlot)
        {
            void WriteSingleFile(string name, IDataWriter writer)
            {
                if (!gameData.Files.ContainsKey(name))
                {
                    gameData.Files.Add(name, FileReader.CreateRawFile(name, writer.ToArray()));
                }
                else
                {
                    gameData.Files[name].Files[1] = new DataReader(writer.ToArray());
                }
            }

            void WriteFile(string name, IDataWriter writer, int fileIndex)
            {
                if (!gameData.Files.ContainsKey(name))
                {
                    gameData.Files.Add(name, FileReader.CreateRawContainer(name, new Dictionary<int, byte[]> { { fileIndex, writer.ToArray() } }));
                }
                else
                {
                    gameData.Files[name].Files[fileIndex] = new DataReader(writer.ToArray());
                }
            }

            void WriteFiles(string name, Dictionary<int, IDataWriter> writers)
            {
                foreach (var writer in writers)
                    WriteFile(name, writer.Value, writer.Key);
            }

            WriteSingleFile($"Save.{saveSlot:00}/Party_data.sav", savegameFiles.SaveDataWriter);
            WriteFiles($"Save.{saveSlot:00}/Party_char.amb", savegameFiles.PartyMemberDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Chest_data.amb", savegameFiles.ChestDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Merchant_data.amb", savegameFiles.MerchantDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Automap.amb", savegameFiles.AutomapDataWriters);
        }

        void SaveToPath(string path, SavegameOutputFiles savegameFiles, int saveSlot, IFileContainer savesContainer)
        {
            void WriteFile(string name, IDataWriter writer)
            {
                var output = new DataWriter();
                var fullPath = Path.Combine(path, name);
                FileWriter.WriteContainer(output, new Dictionary<uint, byte[]> { { 1u, writer.ToArray() } }, FileType.AMBR);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, output.ToArray());
            }

            void WriteFiles(string name, Dictionary<int, IDataWriter> writers)
            {
                var output = new DataWriter();
                FileWriter.WriteContainer(output, writers.ToDictionary(w => (uint)w.Key, w => w.Value.ToArray()), FileType.AMBR);
                File.WriteAllBytes(Path.Combine(path, name), output.ToArray());
            }

            WriteFile($"Save.{saveSlot:00}/Party_data.sav", savegameFiles.SaveDataWriter);
            WriteFiles($"Save.{saveSlot:00}/Party_char.amb", savegameFiles.PartyMemberDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Chest_data.amb", savegameFiles.ChestDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Merchant_data.amb", savegameFiles.MerchantDataWriters);
            WriteFiles($"Save.{saveSlot:00}/Automap.amb", savegameFiles.AutomapDataWriters);

            var savesWriter = new DataWriter();
            FileWriter.Write(savesWriter, savesContainer);
            File.WriteAllBytes(Path.Combine(path, "Saves"), savesWriter.ToArray());
        }
    }
}
