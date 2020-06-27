namespace Ambermoon.Data.Legacy.Characters
{
    public class PartyMemberReader : CharacterReader, IPartyMemberReader
    {
        public void ReadPartyMember(PartyMember partyMember, IDataReader dataReader)
        {
            ReadCharacter(partyMember, dataReader);
        }
    }
}
