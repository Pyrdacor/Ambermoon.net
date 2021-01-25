namespace Ambermoon.Data.Pyrdacor.Serialization
{
    public enum CompressionMethod
    {
        None = 0,
        /// <summary>
        /// Used in sprites with large transparent areas.
        /// Every 0-byte is RLE-encoded by appending the amount
        /// of following 0-bytes in a row. Only the first 0-byte
        /// is stored and then the amount as a byte. So up to
        /// 255 following 0-bytes can be encoded at max.
        /// Other bytes are not encoded.
        /// </summary>
        RLE0 = 1,
        /// <summary>
        /// General purpose lossless compression with fast
        /// decompression.
        /// </summary>
        LZ4 = 2,
        /// <summary>
        /// A modified version of Apple's PackBits algorithm.
        /// Each block starts with a signed byte.
        /// If value > 0 this amount of raw literals follow. 1-127 literals.
        /// If value >= -100 the following byte is repeated (103 + value) times. 3-103 literals.
        /// If value is -114 to -101 the following word is repeated (116 + value) times. 2-15 * 2 literals.
        /// If value is -128 to -115 the following dword is repeated (130 + value) times. 2-15 * 4 literals.
        /// </summary>
        RLE = 3
    }
}
