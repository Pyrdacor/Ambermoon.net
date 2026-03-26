using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor;

partial class GameData : ISavegameSerializer
{
    public static void SaveGame(IDataWriter dataWriter, Savegame savegame)
    {
        dataWriter.WriteWithoutLength("PYSG");

        void WriteSectionFiles<T, TFileSpec>(Dictionary<uint, T> entries, Func<T, TFileSpec> specProvider)
            where TFileSpec : IFileSpec, new()
        {
            PADP.Write(dataWriter, entries.OrderBy(e => e.Key).Select(e => specProvider(e.Value)));
        }

        var saveDataWriter = new DataWriter();
        var saveDataSpec = new FileSpecs.SavegameData(savegame);
        saveDataSpec.Write(saveDataWriter);

        var compressedSaveData = new RLE0Compression().Compress(saveDataWriter.ToArray());

        dataWriter.Write((uint)compressedSaveData.Length);
        dataWriter.Write(compressedSaveData);        

        WriteSectionFiles(savegame.PartyMembers, p => new CharacterData(p));
        WriteSectionFiles(savegame.Chests, c => new ChestData(c));
        WriteSectionFiles(savegame.Merchants, m => new MerchantData(m));
        WriteSectionFiles(savegame.Automaps, a => new ExplorationData(a));
    }

    public static Savegame LoadGame(IDataReader dataReader, GameData gameData)
    {
        if (dataReader.ReadString(4) != "PYSG")
            throw new InvalidDataException("Unsupported savegame format.");

        var savegame = new Savegame();

        int compressedSaveDataSize = (int)(dataReader.ReadDword() & int.MaxValue);
        var compressedSaveData = dataReader.ReadBytes(compressedSaveDataSize);
        var saveDataReader = new RLE0Compression().Decompress(dataReader);

        FileSpecs.SavegameData.ReadInto(savegame, saveDataReader, 0, null!, 0);

        void ReadSectionFiles<T, TFileSpec>(Dictionary<uint, T> target, Func<TFileSpec, T> fileProvider)
            where TFileSpec : IFileSpec, new()
        {
            foreach (var entry in PADP.Read<TFileSpec>(dataReader, gameData))
            {
                target.Add(entry.Key, fileProvider(entry.Value));
            }
        }

        ReadSectionFiles<PartyMember, CharacterData>(savegame.PartyMembers, c => (c.Character as PartyMember)!);
        ReadSectionFiles<Chest, ChestData>(savegame.Chests, c => c.Chest);
        ReadSectionFiles<Merchant, MerchantData>(savegame.Merchants, m => m.Merchant);
        ReadSectionFiles<Automap, ExplorationData>(savegame.Automaps, e => e.Automap);

        return savegame;
    }

    public Savegame LoadInitial()
    {
        var savegame = savegameLoader.Load();

        var initialPartyMembers = partyLoader.LoadAll();
        var initialChests = initialChestLoader.LoadAll();
        var initialMerchants = initialMerchantLoader.LoadAll();
        var initialAutomaps = initialAutomapLoader.LoadAll();

        foreach (var initialPartyMember in initialPartyMembers)
            savegame.PartyMembers.Add(initialPartyMember.Key, initialPartyMember.Value);
        foreach (var initialChest in initialChests)
            savegame.Chests.Add(initialChest.Key, initialChest.Value);
        foreach (var initialMerchant in initialMerchants)
            savegame.Merchants.Add(initialMerchant.Key, initialMerchant.Value);
        foreach (var initialAutomap in initialAutomaps)
            savegame.Automaps.Add(initialAutomap.Key, initialAutomap.Value);

        return savegame;
    }

    public void Read(Savegame savegame, SavegameInputFiles files, IFileContainer partyTextsContainer, IFileContainer? fallbackPartyMemberContainer = null)
    {
        savegame.PartyMembers.Clear();
        savegame.Chests.Clear();
        savegame.Merchants.Clear();
        savegame.Automaps.Clear();

        files.SaveDataReader.Position = 0;
        FileSpecs.SavegameData.ReadInto(savegame, files.SaveDataReader, 0, this, FileSpecs.SavegameData.SupportedVersion);

        foreach (var partyMemberDataReader in files.PartyMemberDataReaders.Files.Where(f => f.Value.Size != 0))
        {
            var partyMemberSpec = new CharacterData();
            var partyMemberTexts = partyTextLoader.LoadOrDefault((ushort)partyMemberDataReader.Key, new Objects.TextList([]));

            partyMemberDataReader.Value.Position = 0;
            partyMemberSpec.Read(partyMemberDataReader.Value, (uint)partyMemberDataReader.Key, this, CharacterData.SupportedVersion);

            var partyMember = (partyMemberSpec.Character as PartyMember)!;
            partyMember.Texts = partyMemberTexts.ToList();

            savegame.PartyMembers.Add((uint)partyMemberDataReader.Key, partyMember);
        }

        foreach (var chestDataReader in files.ChestDataReaders.Files.Where(f => f.Value.Size != 0))
        {
            var chestSpec = new ChestData();

            chestDataReader.Value.Position = 0;
            chestSpec.Read(chestDataReader.Value, (uint)chestDataReader.Key, this, ChestData.SupportedVersion);

            savegame.Chests.Add((uint)chestDataReader.Key, chestSpec.Chest);
        }

        foreach (var merchantDataReader in files.MerchantDataReaders.Files.Where(f => f.Value.Size != 0))
        {
            var merchantSpec = new MerchantData();

            merchantDataReader.Value.Position = 0;
            merchantSpec.Read(merchantDataReader.Value, (uint)merchantDataReader.Key, this, MerchantData.SupportedVersion);

            savegame.Merchants.Add((uint)merchantDataReader.Key, merchantSpec.Merchant);
        }

        foreach (var automapDataReader in files.AutomapDataReaders.Files.Where(f => f.Value.Size != 0))
        {
            var explorationSpec = new ExplorationData();

            automapDataReader.Value.Position = 0;
            explorationSpec.Read(automapDataReader.Value, (uint)automapDataReader.Key, this, ExplorationData.SupportedVersion);

            savegame.Automaps.Add((uint)automapDataReader.Key, explorationSpec.Automap);
        }
    }

    public SavegameOutputFiles Write(Savegame savegame)
    {
        var saveFiles = new SavegameOutputFiles();

        saveFiles.SaveDataWriter = new DataWriter();
        var saveDataSpec = new FileSpecs.SavegameData(savegame);
        saveDataSpec.Write(saveFiles.SaveDataWriter);

        saveFiles.AutomapDataWriters = savegame.Automaps.ToDictionary
        (
            automap => (int)automap.Key,
            automap =>
            {
                var dataWriter = new DataWriter();
                var spec = new ExplorationData(automap.Value);

                spec.Write(dataWriter);

                return (IDataWriter)dataWriter;
            }
        );

        saveFiles.ChestDataWriters = savegame.Chests.ToDictionary
        (
            chest => (int)chest.Key,
            chest =>
            {
                var dataWriter = new DataWriter();
                var spec = new ChestData(chest.Value);

                spec.Write(dataWriter);

                return (IDataWriter)dataWriter;
            }
        );

        saveFiles.MerchantDataWriters = savegame.Merchants.ToDictionary
        (
            merchant => (int)merchant.Key,
            merchant =>
            {
                var dataWriter = new DataWriter();
                var spec = new MerchantData(merchant.Value);

                spec.Write(dataWriter);

                return (IDataWriter)dataWriter;
            }
        );

        saveFiles.PartyMemberDataWriters = savegame.PartyMembers.ToDictionary
        (
            partyMember => (int)partyMember.Key,
            partyMember =>
            {
                var dataWriter = new DataWriter();
                var spec = new CharacterData(partyMember.Value);

                spec.Write(dataWriter);

                return (IDataWriter)dataWriter;
            }
        );

        return saveFiles;
    }
}
