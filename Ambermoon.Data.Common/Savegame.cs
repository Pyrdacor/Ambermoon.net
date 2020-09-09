using System.Collections.Generic;

namespace Ambermoon.Data
{
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
