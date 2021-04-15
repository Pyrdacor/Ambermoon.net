using System.Collections.Generic;

namespace Ambermoon.Data.Serialization
{
    public struct SavegameInputFiles
    {
        /// <summary>
        /// Party_data.sav
        /// </summary>
        public IDataReader SaveDataReader;
        /// <summary>
        /// Party_char.amb
        /// </summary>
        public IFileContainer PartyMemberDataReaders;
        /// <summary>
        /// Chest_data.amb
        /// </summary>
        public IFileContainer ChestDataReaders;
        /// <summary>
        /// Merchant_data.amb
        /// </summary>
        public IFileContainer MerchantDataReaders;
        /// <summary>
        /// Automap.amb
        /// </summary>
        public IFileContainer AutomapDataReaders;
    }

    public struct SavegameOutputFiles
    {
        /// <summary>
        /// Party_data.sav
        /// </summary>
        public IDataWriter SaveDataWriter;
        /// <summary>
        /// Party_char.amb
        /// </summary>
        public Dictionary<int, IDataWriter> PartyMemberDataWriters;
        /// <summary>
        /// Chest_data.amb
        /// </summary>
        public Dictionary<int, IDataWriter> ChestDataWriters;
        /// <summary>
        /// Merchant_data.amb
        /// </summary>
        public Dictionary<int, IDataWriter> MerchantDataWriters;
        /// <summary>
        /// Automap.amb
        /// </summary>
        public Dictionary<int, IDataWriter> AutomapDataWriters;
    }

    public interface ISavegameSerializer
    {
        void Read(Savegame savegame, SavegameInputFiles files, IFileContainer partyTextsContainer,
            IFileContainer fallbackPartyMemberContainer = null);
        SavegameOutputFiles Write(Savegame savegame);
    }
}
