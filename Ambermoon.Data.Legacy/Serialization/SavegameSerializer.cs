using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class SavegameSerializer : ISavegameSerializer
    {
        void ReadSaveData(Savegame savegame, IDataReader dataReader)
        {
            savegame.Year = dataReader.ReadWord();
            savegame.Month = dataReader.ReadWord();
            savegame.DayOfMonth = dataReader.ReadWord();
            savegame.Hour = dataReader.ReadWord();
            savegame.Minute = dataReader.ReadWord();
            savegame.CurrentMapIndex = dataReader.ReadWord();
            savegame.CurrentMapX = dataReader.ReadWord();
            savegame.CurrentMapY = dataReader.ReadWord();
            savegame.CharacterDirection = (CharacterDirection)dataReader.ReadWord();

            // Active spells (2 words each)
            // First word: duration in 5 minute chunks. So 120 means 120 * 5 minutes = 600 minutes = 10h
            // Second word: level (e.g. for light: 1 = magic torch, 2 = magic lantern, 3 = magic sun)
            // An active light spell replaces an existing one.
            // The active spells are in fixed spots:
            // - 0: Light (candle)
            // - 1: Magic barrier (shield)
            // - 2: Magic attack (sword)
            // - 3: Anti-magic barrier (star)
            // - 4: Clairvoyance (eye)
            // - 5: Magic map (map)
            foreach (var activeSpellType in Enum.GetValues<ActiveSpellType>())
            {
                var duration = dataReader.ReadWord();

                if (duration == 0)
                {
                    savegame.ActiveSpells[(int)activeSpellType] = null;
                    dataReader.Position += 2;
                }
                else
                    savegame.ActivateSpell(activeSpellType, duration, dataReader.ReadWord());
            }

            dataReader.ReadWord(); // Number of party members. We don't really need it.
            savegame.ActivePartyMemberSlot = dataReader.ReadWord() - 1; // it is stored 1-based

            for (int i = 0; i < 6; ++i)
                savegame.CurrentPartyMemberIndices[i] = dataReader.ReadWord();

            // TODO: Unknown word
            dataReader.ReadWord();
            savegame.TravelType = (TravelType)dataReader.ReadWord();
            savegame.SpecialItemsActive = dataReader.ReadWord();
            savegame.GameOptions = dataReader.ReadWord();
            savegame.HoursWithoutSleep = dataReader.ReadWord();

            // up to 32 transport positions
            for (int i = 0; i < 32; ++i)
            {
                var type = dataReader.ReadByte();

                if (type == 0)
                {
                    savegame.TransportLocations[i] = null;
                    dataReader.Position += 5;
                }
                else
                {
                    var x = dataReader.ReadByte();
                    var y = dataReader.ReadByte();
                    dataReader.Position += 1; // unknown byte
                    var mapIndex = dataReader.ReadWord();

                    savegame.TransportLocations[i] = new TransportLocation
                    {
                        TravelType = (TravelType)type,
                        MapIndex = mapIndex,
                        Position = new Position(x, y)
                    };
                }
            }


            // global variables (at offset 0x0104, 1024 bytes = 8192 bits = 8192 variables)
            // note that wind gate repair status is also handled by global variables (at offset 0x0112)
            Buffer.BlockCopy(dataReader.ReadBytes(1024), 0, savegame.GlobalVariables, 0, 1024);

            // map event bits. each bit stands for a event. order is 76543210 FECDBA98 ...
            for (int i = 0; i < 1024; ++i)
                savegame.MapEventBits[i] = dataReader.ReadQword();

            // character event bits. each bit stands for a character. order is 76543210 FECDBA98 ...
            for (int i = 0; i < 1024; ++i)
                savegame.CharacterBits[i] = dataReader.ReadDword();

            Buffer.BlockCopy(dataReader.ReadBytes(15), 0, savegame.DictionaryWords, 0, 15);

            // TODO: From start of dictionary words to goto points there are 128 bytes.
            // The dictionary words need at least 15 bytes but most likely they also
            // use 32 bytes or maybe even more.
            dataReader.Position = 0x3584;

            savegame.GotoPointBits = dataReader.ReadBytes(32); // 32 bytes for goto points (256 bits). Each goto point stores the bit index (0-based).
            savegame.ChestUnlockStates = dataReader.ReadBytes(32); // 32 * 8 bits = 256 bits (1 for each chest, possible chest indices 0 to 255)
            savegame.DoorUnlockStates = dataReader.ReadBytes(32); // 32 * 8 bits = 256 bits (1 for each door, possible door indices 0 to 255)

            Buffer.BlockCopy(dataReader.ReadBytes(6), 0, savegame.BattlePositions, 0, 6);

            savegame.TileChangeEvents.Clear();

            while (dataReader.Position < dataReader.Size)
            {
                var mapIndex = dataReader.ReadWord();

                if (mapIndex == 0) // end
                    break;

                var x = dataReader.ReadByte();
                var y = dataReader.ReadByte();
                var tileIndex = dataReader.ReadWord();

                if (mapIndex < 0x0300 && tileIndex < 0xffff) // should be a real map and a valid front tile index
                {
                    savegame.TileChangeEvents.SafeAdd(mapIndex, new ChangeTileEvent
                    {
                        Type = EventType.ChangeTile,
                        Index = uint.MaxValue,
                        MapIndex = mapIndex,
                        X = x,
                        Y = y,
                        FrontTileIndex = tileIndex
                    });
                }
                else
                {
                    // TODO: Does this happen?
                }
            }
        }

        void WriteSaveData(Savegame savegame, IDataWriter dataWriter)
        {
            int startOffset = dataWriter.Position;

            dataWriter.Write((ushort)savegame.Year);
            dataWriter.Write((ushort)savegame.Month);
            dataWriter.Write((ushort)savegame.DayOfMonth);
            dataWriter.Write((ushort)savegame.Hour);
            dataWriter.Write((ushort)savegame.Minute);
            dataWriter.Write((ushort)savegame.CurrentMapIndex);
            dataWriter.Write((ushort)savegame.CurrentMapX);
            dataWriter.Write((ushort)savegame.CurrentMapY);
            dataWriter.Write((ushort)savegame.CharacterDirection);

            foreach (var activeSpellType in Enum.GetValues<ActiveSpellType>())
            {
                var activeSpell = savegame.ActiveSpells[(int)activeSpellType];
                dataWriter.Write((ushort)(activeSpell?.Duration ?? 0));
                dataWriter.Write((ushort)(activeSpell?.Level ?? 0));
            }

            dataWriter.Write((ushort)savegame.CurrentPartyMemberIndices.Count(i => i != 0)); // party member count
            dataWriter.Write((ushort)(1 + savegame.ActivePartyMemberSlot));

            for (int i = 0; i < 6; ++i)
                dataWriter.Write((ushort)savegame.CurrentPartyMemberIndices[i]);

            // TODO: Unknown word
            dataWriter.Write((ushort)0);
            dataWriter.Write((ushort)savegame.TravelType);
            dataWriter.Write((ushort)savegame.SpecialItemsActive);
            dataWriter.Write((ushort)savegame.GameOptions);
            dataWriter.Write((ushort)savegame.HoursWithoutSleep);

            // up to 32 transport positions (6 bytes each)
            for (int i = 0; i < 32; ++i)
            {
                if (savegame.TransportLocations[i] == null)
                {
                    // 6 zero bytes
                    dataWriter.Write((uint)0);
                    dataWriter.Write((ushort)0);
                }
                else
                {
                    dataWriter.Write((byte)savegame.TransportLocations[i].TravelType);
                    dataWriter.Write((byte)savegame.TransportLocations[i].Position.X);
                    dataWriter.Write((byte)savegame.TransportLocations[i].Position.Y);
                    dataWriter.Write((byte)0); // unknown byte
                    dataWriter.Write((ushort)savegame.TransportLocations[i].MapIndex);
                }
            }

            // global variables (at offset 0x0104, 1024 bytes = 8192 bits = 8192 variables)
            dataWriter.Write(savegame.GlobalVariables);

            // map event bits. each bit stands for a event. order is 76543210 FECDBA98 ...
            for (int i = 0; i < 1024; ++i)
                dataWriter.Write(savegame.MapEventBits[i]);

            // character event bits. each bit stands for a character. order is 76543210 FECDBA98 ...
            for (int i = 0; i < 1024; ++i)
                dataWriter.Write(savegame.CharacterBits[i]);

            dataWriter.Write(savegame.DictionaryWords);

            int unknownBytes = 0x3584 - dataWriter.Position;
            dataWriter.Write(Enumerable.Repeat((byte)0, unknownBytes).ToArray());

            dataWriter.Write(savegame.GotoPointBits);
            dataWriter.Write(savegame.ChestUnlockStates);
            dataWriter.Write(savegame.DoorUnlockStates);

            unknownBytes = 0x35e4 - dataWriter.Position;
            dataWriter.Write(Enumerable.Repeat((byte)0, unknownBytes).ToArray());

            dataWriter.Write(savegame.BattlePositions);

            foreach (var tileChangeEvents in savegame.TileChangeEvents)
            {
                foreach (var tileChangeEvent in tileChangeEvents.Value)
                {
                    dataWriter.Write((ushort)tileChangeEvent.MapIndex);
                    dataWriter.Write((byte)tileChangeEvent.X);
                    dataWriter.Write((byte)tileChangeEvent.Y);
                    dataWriter.Write(ConvertEventTileIndex(tileChangeEvent));
                }
            }

            dataWriter.Write((ushort)0); // end marker

            ushort ConvertEventTileIndex(ChangeTileEvent changeTileEvent)
            {
                // TODO: the savegame stores some other events too
                return (ushort)changeTileEvent.FrontTileIndex;
            }

            // TODO: save other data to Party_data.sav
        }

        public void Read(Savegame savegame, SavegameInputFiles files, IFileContainer partyTextsContainer,
            IFileContainer fallbackPartyMemberContainer = null)
        {
            var partyMemberReader = new Characters.PartyMemberReader();
            var chestReader = new ChestReader();
            var merchantReader = new MerchantReader();
            var automapReader = new AutomapReader();

            savegame.PartyMembers.Clear();
            savegame.Chests.Clear();
            savegame.Merchants.Clear();
            savegame.Automaps.Clear();

            foreach (var partyMemberDataReader in files.PartyMemberDataReaders.Files)
            {
                var partyTextFile = partyTextsContainer.Files.ContainsKey(partyMemberDataReader.Key)
                    ? partyTextsContainer.Files[partyMemberDataReader.Key] : null;
                partyMemberDataReader.Value.Position = 0;
                savegame.PartyMembers.Add((uint)partyMemberDataReader.Key,
                    PartyMember.Load((uint)partyMemberDataReader.Key, partyMemberReader,
                        partyMemberDataReader.Value, partyTextFile,
                            fallbackPartyMemberContainer?.Files[partyMemberDataReader.Key]));
            }
            foreach (var chestDataReader in files.ChestDataReaders.Files)
            {
                chestDataReader.Value.Position = 0;
                savegame.Chests.Add((uint)chestDataReader.Key, Chest.Load(chestReader, chestDataReader.Value));
            }
            foreach (var merchantDataReader in files.MerchantDataReaders.Files)
            {
                merchantDataReader.Value.Position = 0;
                savegame.Merchants.Add((uint)merchantDataReader.Key, Merchant.Load(merchantReader, merchantDataReader.Value));
            }
            foreach (var automapDataReader in files.AutomapDataReaders.Files)
            {
                automapDataReader.Value.Position = 0;
                savegame.Automaps.Add((uint)automapDataReader.Key, Automap.Load(automapReader, automapDataReader.Value));
            }

            files.SaveDataReader.Position = 0;
            ReadSaveData(savegame, files.SaveDataReader);
        }

        public SavegameOutputFiles Write(Savegame savegame)
        {
            var files = new SavegameOutputFiles();
            var partyMemberWriter = new Characters.PartyMemberWriter();
            var chestWriter = new ChestWriter();
            var merchantWriter = new MerchantWriter();
            var automapWriter = new AutomapWriter();

            Dictionary<int, IDataWriter> WriteContainer<T>(Dictionary<uint, T> collection, Action<IDataWriter, T> writer)
            {
                var container = new Dictionary<int, IDataWriter>(collection.Count);
                foreach (var item in collection)
                {
                    var valueWriter = new DataWriter();
                    writer(valueWriter, item.Value);
                    container.Add((int)item.Key, valueWriter);
                }
                return container;
            }

            files.PartyMemberDataWriters = WriteContainer(savegame.PartyMembers, (w, p) => partyMemberWriter.WritePartyMember(p, w));
            files.ChestDataWriters = WriteContainer(savegame.Chests, (w, c) => chestWriter.WriteChest(c, w));
            files.MerchantDataWriters = WriteContainer(savegame.Merchants, (w, m) => merchantWriter.WriteMerchant(m, w));
            files.AutomapDataWriters = WriteContainer(savegame.Automaps, (w, a) => automapWriter.WriteAutomap(a, w));
            files.SaveDataWriter = new DataWriter();
            WriteSaveData(savegame, files.SaveDataWriter);

            return files;
        }

        internal static string[] GetSavegameNames(IDataReader savesReader, ref int current)
        {
            savesReader.Position = 0;
            current = savesReader.ReadWord();
            var savegameNames = new string[10];
            int position = savesReader.Position;

            for (int i = 0; i < 10; ++i)
            {
                savegameNames[i] = savesReader.ReadNullTerminatedString();

                if (i < 9) // This is a workaround as some older game versions have fewer bytes for last savegame
                {
                    position += 39;
                    savesReader.Position = position;
                }
            }

            return savegameNames;
        }

        internal static void WriteSavegameName(IGameData gameData, int slot, ref string name)
        {
            if (slot == 0)
                throw new AmbermoonException(ExceptionScope.Application, "Savegame slots must be 1-based");

            if (name.Length > 38)
                name = name.Substring(0, 38);

            byte[] ConvertName(string name)
            {
                List<byte> buffer = new List<byte>(39);
                buffer.AddRange(new AmbermoonEncoding().GetBytes(name));
                for (int i = buffer.Count; i < 39; ++i)
                    buffer.Add((byte)0);
                return buffer.ToArray();
            }

            void WriteCurrentSlot(byte[] data, int slot)
            {
                data[0] = (byte)((slot >> 8) & 0xff);
                data[1] = (byte)(slot & 0xff);
            }

            var nameData = ConvertName(name);
            var currentSlot = slot--;
            const int DataSize = 2 + 10 * 39;

            if (!gameData.Files.ContainsKey("Saves"))
            {
                byte[] data = new byte[DataSize];
                WriteCurrentSlot(data, currentSlot);
                Buffer.BlockCopy(nameData, 0, data, 2 + slot * 39, 39);
                gameData.Files.Add("Saves", FileReader.CreateRawFile("Saves", data));
            }
            else
            {
                // Note: There is a bug in original where the file 'Saves' is
                // too small. If we detect it, we will fix it.
                var data = gameData.Files["Saves"].Files[1].ToArray();
                if (data.Length < DataSize)
                {
                    var tempData = new byte[DataSize];
                    Buffer.BlockCopy(data, 0, tempData, 0, data.Length);
                    data = tempData;
                }
                WriteCurrentSlot(data, currentSlot);
                Buffer.BlockCopy(nameData, 0, data, 2 + slot * 39, 39);
                gameData.Files["Saves"].Files[1] = new DataReader(data);
            }
        }
    }
}
