namespace Ambermoon.Data.Serialization
{
    public struct SavegameFiles
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

    public interface ISavegameSerializer
    {
        void Read(Savegame savegame, SavegameFiles files, IFileContainer partyTextsContainer);
        void Write(Savegame savegame, SavegameFiles files);
    }
}
