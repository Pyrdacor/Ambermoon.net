using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    /// <summary>
    /// Location of a horse, raft, ship, etc.
    /// </summary>
    public class TransportLocation
    {
        public TravelType TravelType;
        public uint MapIndex;
        public Position Position;
    }

    public class ActiveSpell
    {
        public ActiveSpellType Type;
        public uint Duration; // in 5 minute chunks
        public uint Level;
    }

    public class Savegame
    {
        #region Map

        public Dictionary<uint, Automap> Automaps { get; } = new Dictionary<uint, Automap>();
        public uint CurrentMapIndex { get; set; }
        public uint CurrentMapX { get; set; }
        public uint CurrentMapY { get; set; }
        /// <summary>
        /// Note: Also in 3D only 4 directions are saved. So if
        /// you save and look in diagonal direction you will
        /// look in another direction after loading in original game.
        /// </summary>
        public CharacterDirection CharacterDirection { get; set; }
        public TravelType TravelType { get; set; }
        public TransportLocation[] TransportLocations { get; } = new TransportLocation[32];
        public byte[] GlobalVariables { get; } = new byte[1024];
        public bool GetGlobalVariable(uint index)
        {
            if (index > 8192)
                throw new IndexOutOfRangeException("Variable index must be between 0 and 8192");

            uint byteIndex = index / 8;
            uint bitIndex = index % 8;

            return (GlobalVariables[byteIndex] & (1 << (int)bitIndex)) != 0;
        }
        public void SetGlobalVariable(uint index, bool value)
        {
            if (index > 8192)
                throw new IndexOutOfRangeException("Variable index must be between 0 and 8192");

            uint byteIndex = index / 8;
            uint bitIndex = index % 8;
            uint mask = 1u << (int)bitIndex;

            if (value)
                GlobalVariables[byteIndex] |= (byte)mask;
            else
                GlobalVariables[byteIndex] &= (byte)~mask;
        }
        /// <summary>
        /// 64 events bits per map.
        /// Each bit activates (0) or deactivates (1) an event.
        /// The bit index corresponds to the event list index (0 to 63).
        /// 76543210 FEDCBA98 ...
        /// </summary>
        public ulong[] MapEventBits { get; } = new ulong[1024]; // valid maps are 1 to 528, but these bits allow for maps 1 to 1024
        public bool IsEventActive(uint mapIndex, uint eventIndex) => !GetEventBit(mapIndex, eventIndex);
        public void ActivateEvent(uint mapIndex, uint eventIndex, bool activate) => SetEventBit(mapIndex, eventIndex, !activate);
        public bool GetEventBit(uint mapIndex, uint eventIndex)
        {
            if (mapIndex < 1 || mapIndex > 1024)
                throw new IndexOutOfRangeException("Map index must be between 1 and 1024");
            if (eventIndex > 63)
                throw new IndexOutOfRangeException("Event index must be between 0 and 63");

            int byteIndex = (int)eventIndex / 8;
            int bitIndex = (int)eventIndex % 8;
            ulong bitValue = 1ul << ((7 - byteIndex) * 8 + bitIndex);

            return (MapEventBits[mapIndex - 1] & bitValue) != 0;
        }
        public void SetEventBit(uint mapIndex, uint eventIndex, bool bit)
        {
            if (mapIndex < 1 || mapIndex > 1024)
                throw new IndexOutOfRangeException("Map index must be between 1 and 1024");
            if (eventIndex > 63)
                throw new IndexOutOfRangeException("Event index must be between 0 and 63");

            int byteIndex = (int)eventIndex / 8;
            int bitIndex = (int)eventIndex % 8;
            ulong bitValue = 1ul << ((7 - byteIndex) * 8 + bitIndex);

            if (bit)
                MapEventBits[mapIndex - 1] |= bitValue;
            else
                MapEventBits[mapIndex - 1] &= ~bitValue;
        }
        /// <summary>
        /// 32 events bits per map.
        /// Each bit disables (1) or enables (0) a character on the map.
        /// The bit index corresponds to the character index (0 to 31).
        /// </summary>
        public uint[] CharacterBits { get; } = new uint[1024]; // valid maps are 1 to 528, but these bits allow for maps 1 to 1024
        public bool GetCharacterBit(uint mapIndex, uint characterIndex)
        {
            if (mapIndex < 1 || mapIndex > 1024)
                throw new IndexOutOfRangeException("Map index must be between 1 and 1024");
            if (characterIndex > 31)
                throw new IndexOutOfRangeException("Character index must be between 0 and 31");

            int byteIndex = (int)characterIndex / 8;
            int bitIndex = (int)characterIndex % 8;
            uint bitValue = 1u << ((3 - byteIndex) * 8 + bitIndex);
            return (CharacterBits[mapIndex - 1] & bitValue) != 0;
        }
        public void SetCharacterBit(uint mapIndex, uint characterIndex, bool bit)
        {
            if (mapIndex < 1 || mapIndex > 1024)
                throw new IndexOutOfRangeException("Map index must be between 1 and 1024");
            if (characterIndex > 31)
                throw new IndexOutOfRangeException("Character index must be between 0 and 31");

            int byteIndex = (int)characterIndex / 8;
            int bitIndex = (int)characterIndex % 8;
            uint bitValue = 1u << ((3 - byteIndex) * 8 + bitIndex);

            if (bit)
                CharacterBits[mapIndex - 1] |= bitValue;
            else
                CharacterBits[mapIndex - 1] &= ~bitValue;
        }

        public byte[] GotoPointBits { get; set; } = new byte[32];

        public bool IsGotoPointActive(uint index)
        {
            int byteIndex = (int)index / 8;
            int bitIndex = (int)index % 8;
            return (GotoPointBits[byteIndex] & (1 << bitIndex)) != 0;
        }

        public void ActivateGotoPoint(uint index)
        {
            int byteIndex = (int)index / 8;
            int bitIndex = (int)index % 8;
            GotoPointBits[byteIndex] |= (byte)(1u << bitIndex);
        }

        #endregion

        #region Party

        public Dictionary<uint, PartyMember> PartyMembers { get; } = new Dictionary<uint, PartyMember>();
        public uint[] CurrentPartyMemberIndices { get; } = new uint[6];
        public int ActivePartyMemberSlot = 0; // 0 - 5
        public byte[] BattlePositions { get; } = new byte[6];
        public ActiveSpell[] ActiveSpells { get; } = new ActiveSpell[6];
        /// <summary>
        /// Activates a spell.
        /// </summary>
        /// <param name="type">Active spell type</param>
        /// <param name="duration">Duration in 5 minute chunks (e.g. 120 for 10 ingame hours).</param>
        /// <param name="level">Level of the spell or 0 if not used.</param>
        public void ActivateSpell(ActiveSpellType type, uint duration, uint level)
        {
            if (ActiveSpells[(int)type] == null)
            {
                ActiveSpells[(int)type] = new ActiveSpell
                {
                    Type = type,
                    Duration = duration,
                    Level = level
                };
            }
            else
            {
                var activeSpell = ActiveSpells[(int)type];

                if (activeSpell.Level < level)
                    activeSpell.Level = level;
                activeSpell.Duration = Util.Limit(activeSpell.Duration, duration, 200u);
            }
        }
        /// <summary>
        /// Updates all active spells and end them if they run out.
        /// </summary>
        /// <param name="elapsedSinceLastUpdate">Time elapsed since last update in 5 minute chunks.</param>
        public void UpdateActiveSpells(uint elapsedSinceLastUpdate)
        {
            foreach (var type in EnumHelper.GetValues<ActiveSpellType>())
            {
                var activeSpell = ActiveSpells[(int)type];

                if (activeSpell != null)
                {
                    if (activeSpell.Duration <= elapsedSinceLastUpdate)
                        ActiveSpells[(int)type] = null;
                    else
                        activeSpell.Duration -= elapsedSinceLastUpdate;
                }
            }
        }
        public bool IsSpellActive(ActiveSpellType activeSpellType)
        {
            return ActiveSpells.Any(s => s?.Type == activeSpellType && s?.Duration > 0);
        }
        public uint GetActiveSpellLevel(ActiveSpellType activeSpellType)
        {
            return ActiveSpells.Where(s => s?.Type == activeSpellType && s?.Duration > 0).Select(s => s.Level).DefaultIfEmpty(0u).Max();
        }
        public uint GetActiveSpellDuration(ActiveSpellType activeSpellType)
        {
            return ActiveSpells.Where(s => s?.Type == activeSpellType && s?.Duration > 0).Select(s => s.Duration).DefaultIfEmpty(0u).Max();
        }
        /// <summary>
        /// One bit for each available dictionary word.
        /// If the bit is set the word is available in conversations.
        /// </summary>
        public byte[] DictionaryWords { get; set; } = new byte[128];
        public bool IsDictionaryWordKnown(uint index)
        {
            return (DictionaryWords[index / 8] & (1u << (int)(index % 8))) != 0;
        }
        public void AddDictionaryWord(uint index)
        {
            DictionaryWords[index / 8] |= (byte)(1u << (int)(index % 8));
        }

        #endregion

        #region Chests and merchants

        public byte[] ChestUnlockStates { get; set; }
        public byte[] DoorUnlockStates { get; set; }
        public byte[] ExtendedChestUnlockStates { get; set; }
        public bool IsChestLocked(uint chestIndex)
        {
            if (chestIndex > 383)
                throw new IndexOutOfRangeException($"Chest index must be 0..383 but was {chestIndex}.");

            if (chestIndex > 255)
            {
                chestIndex -= 256;
                return (ExtendedChestUnlockStates[chestIndex / 8] & (1 << ((int)chestIndex % 8))) == 0;
            }

            return (ChestUnlockStates[chestIndex / 8] & (1 << ((int)chestIndex % 8))) == 0;
        }
        public void LockChest(uint chestIndex)
        {
            if (chestIndex > 383)
                throw new IndexOutOfRangeException($"Chest index must be 0..383 but was {chestIndex}.");

            if (chestIndex > 255)
            {
                chestIndex -= 256;
                ExtendedChestUnlockStates[chestIndex / 8] &= (byte)~(1 << ((int)chestIndex % 8));
            }
            else
            {
                ChestUnlockStates[chestIndex / 8] &= (byte)~(1 << ((int)chestIndex % 8));
            }
        }
        public void UnlockChest(uint chestIndex)
        {
            if (chestIndex > 383)
                throw new IndexOutOfRangeException($"Chest index must be 0..383 but was {chestIndex}.");

            if (chestIndex > 255)
            {
                chestIndex -= 256;
                ExtendedChestUnlockStates[chestIndex / 8] |= (byte)(1 << ((int)chestIndex % 8));
            }
            else
            {
                ChestUnlockStates[chestIndex / 8] |= (byte)(1 << ((int)chestIndex % 8));
            }
        }
        public bool IsDoorLocked(uint doorIndex)
        {
            if (doorIndex > 127)
                throw new IndexOutOfRangeException($"Door index must be 0..127 but was {doorIndex}.");

            return (DoorUnlockStates[doorIndex / 8] & (1 << ((int)doorIndex % 8))) == 0;
        }
        public void LockDoor(uint doorIndex)
        {
            if (doorIndex > 127)
                throw new IndexOutOfRangeException($"Door index must be 0..127 but was {doorIndex}.");

            DoorUnlockStates[doorIndex / 8] &= (byte)~(1 << ((int)doorIndex % 8));
        }
        public void UnlockDoor(uint doorIndex)
        {
            if (doorIndex > 127)
                throw new IndexOutOfRangeException($"Door index must be 0..127 but was {doorIndex}.");

            DoorUnlockStates[doorIndex / 8] |= (byte)(1 << ((int)doorIndex % 8));
        }
        public Dictionary<uint, Chest> Chests { get; } = new Dictionary<uint, Chest>();
        public Dictionary<uint, Merchant> Merchants { get; } = new Dictionary<uint, Merchant>();

        #endregion

        #region Events

        // After chest locked states there can be tile change events with 6 bytes each.
        // They are encoded as:
        //  word MapIndex;
        //  byte X; // 1-based
        //  byte Y; // 1-based
        //  word NewFrontTileIndex;
        //
        // The key is the map index and the value a list of change tile events
        // which should be executed when entering the map or loading a game which
        // starts on that map.
        public Dictionary<uint, List<ChangeTileEvent>> TileChangeEvents { get; } = new Dictionary<uint, List<ChangeTileEvent>>();

        #endregion

        #region Misc

        public uint Year { get; set; }
        public uint Month { get; set; }
        public uint DayOfMonth { get; set; }
        public uint Hour { get; set; }
        public uint Minute { get; set; } // a multiple of 5
        public uint HoursWithoutSleep { get; set; }
        public uint YearsPassed { get; set; }
        public ushort SpecialItemsActive { get; set; }
        public ushort GameOptions { get; set; }

        public bool IsSpecialItemActive(SpecialItemPurpose specialItemPurpose)
        {
            return (SpecialItemsActive & (1 << (int)specialItemPurpose)) != 0;
        }

        public void ActivateSpecialItem(SpecialItemPurpose specialItemPurpose)
        {
            SpecialItemsActive |= (ushort)(1 << (int)specialItemPurpose);
        }

        public bool IsGameOptionActive(Option option)
        {
            return (GameOptions & (1 << (int)option)) != 0;
        }

        public void SetGameOption(Option option, bool active)
        {
            ushort bit = (ushort)(1 << (int)option);

            if (active)
                GameOptions |= bit;
            else
                GameOptions &= (ushort)~bit;
        }

        #endregion

        public PartyMember GetPartyMember(int slot) => CurrentPartyMemberIndices[slot] == 0 ? null : PartyMembers[CurrentPartyMemberIndices[slot]];

        public static Savegame Load(ISavegameSerializer savegameSerializer, SavegameInputFiles savegameFiles, IFileContainer partyTextsContainer)
        {
            var savegame = new Savegame();

            savegameSerializer.Read(savegame, savegameFiles, partyTextsContainer);

            return savegame;
        }
    }
}
