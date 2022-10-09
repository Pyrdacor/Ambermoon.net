using System.ComponentModel;

namespace Ambermoon.Data.Legacy.Serialization
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
        AMPC = 0x414d5043,
        /// <summary>
        /// Special format. JH combined with LOB. Key in lower word.
        /// </summary>
        JHPlusLOB = 0xaaaa0000,
        /// <summary>
        /// Special format. JH combined with AMBR. Key in lower word.
        /// </summary>
        JHPlusAMBR = 0xbbbb0000,
        /// <summary>
        /// Special text container.
        /// </summary>
        AMTX = 0x414d5458
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class FileTypeExtensions
    {
        public static FileType AsFileType(this uint header)
        {
            var upperHalf = header & 0xffff0000;

            if (upperHalf == (uint)FileType.JH)
                return FileType.JH;
            if (upperHalf == (uint)FileType.JHPlusLOB)
                return FileType.JHPlusLOB;
            if (upperHalf == (uint)FileType.JHPlusAMBR)
                return FileType.JHPlusAMBR;

            return (FileType)header;
        }

        public static bool IsJH(this uint header)
        {
            var fileType = header.AsFileType();

            return fileType == FileType.JH ||
                   fileType == FileType.JHPlusLOB ||
                   fileType == FileType.JHPlusAMBR;
        }
    }
}
