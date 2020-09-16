using Ambermoon.Data.Enumerations;
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

        #endregion

        #region Party members

        public List<PartyMember> PartyMembers { get; } = new List<PartyMember>();
        public int[] CurrentPartyMemberIndices { get; } = new int[6];
        public int ActivePartyMemberSlot = 0; // 0 - 5

        #endregion

        #region Chests and merchants

        public byte[] ChestUnlockStates { get; set; }
        public bool IsChestLocked(uint chestIndex)
        {
            if (chestIndex == 0 || chestIndex > 256)
                throw new System.IndexOutOfRangeException($"Chest index must be 1-256 but was {chestIndex}.");

            return (ChestUnlockStates[chestIndex / 8] & (1 << ((int)chestIndex % 8))) == 0;
        }
        public void UnlockChest(uint chestIndex)
        {
            if (chestIndex == 0 || chestIndex > 256)
                throw new System.IndexOutOfRangeException($"Chest index must be 1-256 but was {chestIndex}.");

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

        public uint Hour { get; set; }
        public uint Minute { get; set; } // a multiple of 5

        #endregion


        // TODO: automap
        // TODO: time, ...

        public PartyMember GetPartyMember(int slot) => CurrentPartyMemberIndices[slot] == 0 ? null : PartyMembers[CurrentPartyMemberIndices[slot] - 1];

        public static Savegame Load(ISavegameSerializer savegameSerializer, SavegameFiles savegameFiles)
        {
            var savegame = new Savegame();

            savegameSerializer.Read(savegame, savegameFiles);

            return savegame;
        }
    }
}
