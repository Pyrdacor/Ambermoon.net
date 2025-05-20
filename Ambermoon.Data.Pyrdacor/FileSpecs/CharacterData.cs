using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class CharacterData : IFileSpec<CharacterData>, IFileSpec
{
    public static string Magic => "CHR";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Character? character = null;

    public Character Character => character!;

    public CharacterData()
    {

    }

    public CharacterData(Character character)
    {
        this.character = character;
    }

    public void Read(IDataReader dataReader, uint index, GameData gameData)
    {
        switch (dataReader.PeekByte())
        {
            case 0: // party member
                // TODO
                throw new NotImplementedException();
            case 1: // NPC
                // TODO
                throw new NotImplementedException();
            case 2: // monster
                // TODO
                throw new NotImplementedException();
            default:
                throw new AmbermoonException(ExceptionScope.Data, "Invalid character data.");
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        if (character == null)
            throw new AmbermoonException(ExceptionScope.Application, "Character data was null when trying to write it.");

        // TODO
        throw new NotImplementedException();
    }
}
