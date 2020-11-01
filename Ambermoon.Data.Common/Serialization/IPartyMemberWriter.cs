namespace Ambermoon.Data.Serialization
{
    public interface IPartyMemberWriter
    {
        void WritePartyMember(PartyMember partyMember, IDataWriter dataWriter);
        void WritePartyMember(PartyMember partyMember, IDataWriter dataWriter, IDataWriter textWriter);
    }
}
