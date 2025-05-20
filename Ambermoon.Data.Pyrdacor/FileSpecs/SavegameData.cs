﻿using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class SavegameData : IFileSpec<SavegameData>, IFileSpec
{
    public static string Magic => "SAV";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();

    public void Read(IDataReader dataReader, uint _, GameData __)
    {
        throw new NotImplementedException();
    }

    public void Write(IDataWriter dataWriter)
    {
        throw new NotImplementedException();
    }
}
