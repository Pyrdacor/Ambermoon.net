namespace Ambermoon.Data
{
    public interface IPartyMemberReader
    {
        void ReadPartyMember(PartyMember partyMember, IDataReader dataReader, IDataReader partyTextReader);
    }
}
