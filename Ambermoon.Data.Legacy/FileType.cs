namespace Ambermoon.Data.Legacy
{
    public enum FileType : uint
    {
        None = 0,
        /// <summary>
        /// JH-encoded files (Jurie Horneman's encoder).
        /// </summary>
        JH = 0x4a480000,
        /// <summary>
        /// LOB-encoded files (Lothar Becks' encoder).
        /// </summary>
        LOB = 0x014c4f42,
        /// <summary>
        /// Another LOB-encoded files.
        /// </summary>
        VOL1 = 0x564f4c31,
        /// <summary>
        /// Crypted AMN file (multiple file container, data uses JH encryption).
        /// </summary>
        AMNC = 0x414d4e43,
        /// <summary>
        /// Packed AMN file (multiple file container, files are often LOB-encoded).
        /// </summary>
        AMNP = 0x414d4e50,
        /// <summary>
        /// Raw AMN file (no encryption).
        /// </summary>
        AMBR = 0x414d4252,
        /// <summary>
        /// Another multiple file container.
        /// </summary>
        AMPC = 0x414d5043
    }
}
