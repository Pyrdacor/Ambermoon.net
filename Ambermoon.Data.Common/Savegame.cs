using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;

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
        public ushort WindGatesActive { get; set; }
        /// <summary>
        /// 64 events bits per map.
        /// Each bit activates (0) or deactivates (1) an event.
        /// The bit index corresponds to the event list index (0 to 63).
        /// </summary>
        public ulong[] MapEventBits { get; } = new ulong[529]; // 0 to 528
        public bool GetEventBit(uint mapIndex, uint eventIndex)
        {
            int byteIndex = (int)eventIndex / 8;
            byte bits = (byte)(MapEventBits[mapIndex] >> (7 - byteIndex) * 8);
            int bitIndex = (int)eventIndex % 8;
            return (bits & (1 << bitIndex)) != 0;
        }
        public void SetEventBit(uint mapIndex, uint eventIndex, bool bit)
        {
            int byteIndex = (int)eventIndex / 8;
            int bitIndex = (int)eventIndex % 8;
            ulong bitValue = 1ul << ((7 - byteIndex) * 8 + bitIndex);

            if (bit)
                MapEventBits[mapIndex] |= bitValue;
            else
                MapEventBits[mapIndex] &= ~bitValue;
        }

        #endregion

        #region Party

        public List<PartyMember> PartyMembers { get; } = new List<PartyMember>();
        public int[] CurrentPartyMemberIndices { get; } = new int[6];
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
                activeSpell.Duration = Math.Min(200u, activeSpell.Duration + duration);
            }
        }
        /// <summary>
        /// Updates all active spells and end them if they run out.
        /// </summary>
        /// <param name="elapsedSinceLastUpdate">Time elapsed since last update in 5 minute chunks.</param>
        public void UpdateActiveSpells(uint elapsedSinceLastUpdate)
        {
            foreach (var type in Enum.GetValues<ActiveSpellType>())
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
        /// <summary>
        /// 14 * 8 bits + 3 bits = 115 bits.
        /// One bit for each available dictionary word.
        /// If the bit is set the word is available in conversations.
        /// </summary>
        public byte[] DictionaryWords { get; } = new byte[15];

        #endregion

        #region Chests and merchants

        public byte[] ChestUnlockStates { get; set; }
        public bool IsChestLocked(uint chestIndex)
        {
            if (chestIndex > 511)
                throw new IndexOutOfRangeException($"Chest index must be 0-511 but was {chestIndex}.");

            return (ChestUnlockStates[chestIndex / 8] & (1 << ((int)chestIndex % 8))) == 0;
        }
        public void UnlockChest(uint chestIndex)
        {
            if (chestIndex > 511)
                throw new IndexOutOfRangeException($"Chest index must be 0-511 but was {chestIndex}.");

            ChestUnlockStates[chestIndex / 8] |= (byte)(1 << ((int)chestIndex % 8));
        }
        public List<Chest> Chests { get; } = new List<Chest>();
        public List<Merchant> Merchants { get; } = new List<Merchant>();

        #endregion

        #region Events

        // TODO: This is work in progress. After chest locked states there can be events with 6 bytes each.
        // I am sure about change tile events. They are encoded as:
        //  word MapIndex;
        //  byte X; // 1-based
        //  byte Y; // 1-based
        //  word NewFrontTileIndex;
        //
        // But I also saw events where NewFrontTileIndex is too big (08 09)
        // or even some where MapIndex is too big (08 0a).
        //
        // I assume that if the first word is not a valid map index (e.g. first byte > 2)
        // that some other event is meant. And if the map event has an invalid front tile
        // index it either is some other event or a new map event index is set to the tile.
        //
        // For now we only load the valid change tile events.
        // The key is the map index and the value a list of change tile events
        // which should be executed when entering the map or loading a game which
        // starts on that map).
        public Dictionary<uint, List<ChangeTileEvent>> TileChangeEvents { get; } = new Dictionary<uint, List<ChangeTileEvent>>();

        #endregion

        #region Misc

        public uint Year { get; set; }
        public uint Month { get; set; }
        public uint DayOfMonth { get; set; }
        public uint Hour { get; set; }
        public uint Minute { get; set; } // a multiple of 5
        public uint HoursWithoutSleep { get; set; }
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


        // TODO: automap
        // TODO: ...

        public PartyMember GetPartyMember(int slot) => CurrentPartyMemberIndices[slot] == 0 ? null : PartyMembers[CurrentPartyMemberIndices[slot] - 1];

        public static Savegame Load(ISavegameSerializer savegameSerializer, SavegameFiles savegameFiles, IFileContainer partyTextsContainer)
        {
            var savegame = new Savegame();

            savegameSerializer.Read(savegame, savegameFiles, partyTextsContainer);

            return savegame;
        }
    }
}
