namespace Ambermoon.Data.Serialization
{
    public interface IPartyMemberReader
    {
        void ReadPartyMember(PartyMember partyMember, IDataReader dataReader,
            IDataReader partyTextReader, IDataReader fallbackDataReader = null);
    }
}
