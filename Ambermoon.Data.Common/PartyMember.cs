namespace Ambermoon.Data
{
    public class PartyMember : Character
    {
        private PartyMember()
            : base(CharacterType.PartyMember)
        {

        }

        public static PartyMember Load(IPartyMemberReader partyMemberReader, IDataReader dataReader)
        {
            var partyMember = new PartyMember();

            partyMemberReader.ReadPartyMember(partyMember, dataReader);

            return partyMember;
        }
    }
}
