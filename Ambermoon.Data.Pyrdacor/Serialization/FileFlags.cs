using System;

namespace Ambermoon.Data.Pyrdacor.Serialization
{
    [Flags]
    public enum FileFlags
    {
        None = 0,
        /// <summary>
        /// If set the file data is compressed. The first byte of the data will
        /// provide the compression method. If <see cref="LanguageDependent"/> is
        /// also set, the compression method byte is the second data byte instead.
        /// </summary>
        Compressed = 0x01,
        /// <summary>
        /// If set the first byte of the data will provide a language identifier.
        /// If <see cref="Compressed"/> is also set, the language identifier will
        /// still be in first place and the compression method byte will come afterwards.
        /// See <see cref="DataLanguage"/> for the values.
        /// This is mostly set for dictionaries and text files.
        /// </summary>
        LanguageDependent = 0x02,
        Reserved1 = 0x04,
        Reserved2 = 0x08
    }
}
