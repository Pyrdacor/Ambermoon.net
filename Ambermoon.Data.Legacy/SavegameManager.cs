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

        public string[] GetSavegameNames(IGameData gameData, out int current, int totalSavegames)
        {
            current = 0;

            if (File.Exists(savesPath))
            {
                return SavegameSerializer.GetSavegameNames(new DataReader(File.ReadAllBytes(savesPath)), ref current);
            }
            else if (!(gameData as ILegacyGameData).Files.ContainsKey("Saves"))
            {
                return Enumerable.Repeat("", totalSavegames).ToArray();
            }
            else
            {
                return SavegameSerializer.GetSavegameNames((gameData as ILegacyGameData).Files["Saves"].Files[1], ref current);
            }            
        }

        public void WriteSavegameName(IGameData gameData, int slot, ref string name)
        {
            SavegameSerializer.WriteSavegameName(gameData, slot, ref name);
        }

        public static readonly string[] SaveFileNames = new string[5]
        {
            "Party_data.sav",
            "Party_char.amb",
            "Chest_data.amb",
            "Merchant_data.amb",
            "Automap.amb"
        };

        public Savegame Load(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, int totalSavegames)
        {
            var legacyGameData = gameData as ILegacyGameData;

            if (!transferredFolderSaves && File.Exists(savesPath))
            {
                transferredFolderSaves = true;
                var folderSaveData = new GameData(GameData.LoadPreference.ForceExtracted, null, false);

                try
                {
                    folderSaveData.Load(path, true);

                    KeyValuePair<string, IFileContainer>? TransferFile(string name)
                    {
                        if (folderSaveData.Files.ContainsKey(name))
                            return KeyValuePair.Create(name, folderSaveData.Files[name]);
                        return null;
                    }

                    for (int i = 1; i <= totalSavegames; ++i)
                    {
                        var saveFiles = new List<KeyValuePair<string, IFileContainer>>(5);
                        foreach (var saveFileName in SaveFileNames)
                        {
                            var file = TransferFile($"Save.{i:00}/{saveFileName}");
                            if (file == null)
                                break;
                            saveFiles.Add(file.Value);
                        }
                        if (saveFiles.Count == 5)
                        {
                            foreach (var saveFile in saveFiles)
                                legacyGameData.Files[saveFile.Key] = saveFile.Value;
                        }
                    }

                    var saves = TransferFile("Saves");

                    if (saves != null)
                        legacyGameData.Files[saves.Value.Key] = saves.Value.Value;
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
                if (saveSlot == 99 && Directory.Exists(Path.Combine(path, "Save.99")))
                {
                    string backupPath = Path.Combine(path, "Save.99");
                    var fileReader = new FileReader();
                    IFileContainer ReadContainer(string name)
                    {
                        using var stream = File.OpenRead(Path.Combine(backupPath, name));
                        return fileReader.ReadRawFile(name, stream);
                    }
                    IDataReader ReadFile(string name)
                    {
                        return ReadContainer(name).Files[1];
                    }
                    savegameFiles = new SavegameInputFiles
                    {
                        SaveDataReader = ReadFile("Party_data.sav"),
                        PartyMemberDataReaders = ReadContainer("Party_char.amb"),
                        ChestDataReaders = ReadContainer("Chest_data.amb"),
                        MerchantDataReaders = ReadContainer("Merchant_data.amb"),
                        AutomapDataReaders = ReadContainer("Automap.amb")
                    };
                }
                else if (saveSlot > 10 && saveSlot <= totalSavegames)
                {
                    if (Directory.Exists(Path.Combine(path, $"Save.{saveSlot:00}")))
                    {
                        string path = Path.Combine(this.path, $"Save.{saveSlot:00}");
                        var fileReader = new FileReader();
                        IFileContainer ReadContainer(string name)
                        {
                            using var stream = File.OpenRead(Path.Combine(path, name));
                            return fileReader.ReadRawFile(name, stream);
                        }
                        IDataReader ReadFile(string name)
                        {
                            return ReadContainer(name).Files[1];
                        }
                        savegameFiles = new SavegameInputFiles
                        {
                            SaveDataReader = ReadFile("Party_data.sav"),
                            PartyMemberDataReaders = ReadContainer("Party_char.amb"),
                            ChestDataReaders = ReadContainer("Chest_data.amb"),
                            MerchantDataReaders = ReadContainer("Merchant_data.amb"),
                            AutomapDataReaders = ReadContainer("Automap.amb")
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    savegameFiles = new SavegameInputFiles
                    {
                        SaveDataReader = legacyGameData.Files[$"Save.{saveSlot:00}/Party_data.sav"].Files[1],
                        PartyMemberDataReaders = legacyGameData.Files[$"Save.{saveSlot:00}/Party_char.amb"],
                        ChestDataReaders = legacyGameData.Files[$"Save.{saveSlot:00}/Chest_data.amb"],
                        MerchantDataReaders = legacyGameData.Files[$"Save.{saveSlot:00}/Merchant_data.amb"],
                        AutomapDataReaders = legacyGameData.Files[$"Save.{saveSlot:00}/Automap.amb"]
                    };
                }
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

            var initialPartyMemberReaders = legacyGameData.Files.TryGetValue("Initial/Party_char.amb", out var readers)
                ? readers : legacyGameData.Files.TryGetValue("Save.00/Party_char.amb", out readers) ? readers : null;

            savegameSerializer.Read(savegame, savegameFiles, legacyGameData.Files["Party_texts.amb"], initialPartyMemberReaders);

            return savegame;
        }

        public Savegame LoadInitial(IGameData gameData, ISavegameSerializer savegameSerializer)
        {
            var legacyGameData = gameData as ILegacyGameData;
            var savegame = new Savegame();
            SavegameInputFiles savegameFiles;
            IFileContainer partyTextContainer;

            try
            {
                savegameFiles = new SavegameInputFiles
                {
                    SaveDataReader = legacyGameData.Files["Initial/Party_data.sav"].Files[1],
                    PartyMemberDataReaders = legacyGameData.Files["Initial/Party_char.amb"],
                    ChestDataReaders = legacyGameData.Files["Initial/Chest_data.amb"],
                    MerchantDataReaders = legacyGameData.Files["Initial/Merchant_data.amb"],
                    AutomapDataReaders = legacyGameData.Files["Initial/Automap.amb"]
                };
            }
            catch
            {
                try
                {
                    savegameFiles = new SavegameInputFiles
                    {
                        SaveDataReader = legacyGameData.Files["Save.00/Party_data.sav"].Files[1],
                        PartyMemberDataReaders = legacyGameData.Files["Save.00/Party_char.amb"],
                        ChestDataReaders = legacyGameData.Files["Save.00/Chest_data.amb"],
                        MerchantDataReaders = legacyGameData.Files["Save.00/Merchant_data.amb"],
                        AutomapDataReaders = legacyGameData.Files["Save.00/Automap.amb"]
                    };
                }
                catch
                {
                    savegameFiles = new SavegameInputFiles
                    {
                        SaveDataReader = legacyGameData.Files["Party_data.sav"].Files[1],
                        PartyMemberDataReaders = legacyGameData.Files["Party_char.amb"],
                        ChestDataReaders = legacyGameData.Files["Chest_data.amb"],
                        MerchantDataReaders = legacyGameData.Files["Merchant_data.amb"],
                        AutomapDataReaders = legacyGameData.Files["Automap.amb"]
                    };
                }
            }

            partyTextContainer = legacyGameData.Files["Party_texts.amb"];
            savegameSerializer.Read(savegame, savegameFiles, partyTextContainer);
            return savegame;
        }

        public bool HasCrashSavegame() => Directory.Exists(Path.Combine(path, "Save.99"));

        public void Save(IGameData gameData, ISavegameSerializer savegameSerializer, int saveSlot, string name, Savegame savegame)
        {
            var savegameFiles = savegameSerializer.Write(savegame);
            if (saveSlot <= 10)
            {
                WriteSavegameName(gameData, saveSlot, ref name);
                SaveToGameData(gameData, savegameFiles, saveSlot);
            }
            SaveToPath(path, savegameFiles, saveSlot, saveSlot > 10 ? null : (gameData as ILegacyGameData).Files["Saves"]);
        }

        public void SetActiveSavegame(IGameData gameData, int slot)
        {
            if (slot < 1)
                throw new AmbermoonException(ExceptionScope.Application, "Savegame slots must be 1-based.");

            if (slot <= 10)
            {
                string name = GetSavegameNames(gameData, out _, 10)[slot - 1];
                WriteSavegameName(gameData, slot, ref name);
                var savesWriter = new DataWriter();
                FileWriter.Write(savesWriter, (gameData as ILegacyGameData).Files["Saves"], Compression.LobCompression.LobType.Ambermoon);
                File.WriteAllBytes(savesPath, savesWriter.ToArray());
            }
        }

        public void SaveCrashedGame(ISavegameSerializer savegameSerializer, Savegame savegame)
        {
            var savegameFiles = savegameSerializer.Write(savegame);
            SaveToPath(path, savegameFiles, 99, null);
        }

        public bool RemoveCrashedSavegame()
        {
            try
            {
                var directory = new DirectoryInfo(Path.Combine(path, "Save.99"));
                foreach (var file in directory.GetFiles())
                {
                    if (file.Name == "Party_data.sav" ||
                        file.Name == "Party_char.amb" ||
                        file.Name == "Chest_data.amb" ||
                        file.Name == "Merchant_data.amb" ||
                        file.Name == "Automap.amb")
                        file.Delete();
                }
                directory.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        void SaveToGameData(IGameData gameData, SavegameOutputFiles savegameFiles, int saveSlot)
        {
            var legacyGameData = gameData as ILegacyGameData;

            void WriteSingleFile(string name, IDataWriter writer)
            {
                if (!legacyGameData.Files.ContainsKey(name))
                {
                    legacyGameData.Files.Add(name, FileReader.CreateRawFile(name, writer.ToArray()));
                }
                else
                {
                    legacyGameData.Files[name].Files[1] = new DataReader(writer.ToArray());
                }
            }

            void WriteFile(string name, IDataWriter writer, int fileIndex)
            {
                if (!legacyGameData.Files.ContainsKey(name))
                {
                    legacyGameData.Files.Add(name, FileReader.CreateRawContainer(name, new Dictionary<int, byte[]> { { fileIndex, writer.ToArray() } }));
                }
                else
                {
                    legacyGameData.Files[name].Files[fileIndex] = new DataReader(writer.ToArray());
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
                var fullPath = Path.Combine(path, name);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, writer.ToArray());
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

            if (savesContainer != null)
            {
                var savesWriter = new DataWriter();
                FileWriter.Write(savesWriter, savesContainer, Compression.LobCompression.LobType.Ambermoon);
                File.WriteAllBytes(savesPath, savesWriter.ToArray());
            }
        }
    }
}
