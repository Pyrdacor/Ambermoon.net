using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class PartyMember : Character, IConversationPartner
    {
        public ushort MarkOfReturnMapIndex { get; set; }
        public ushort MarkOfReturnX { get; set; }
        public ushort MarkOfReturnY { get; set; }
        public List<string> Texts { get; set; }
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();

        private PartyMember()
            : base(CharacterType.PartyMember)
        {

        }

        public static PartyMember Load(uint index, IPartyMemberReader partyMemberReader,
            IDataReader dataReader, IDataReader partyTextReader)
        {
            var partyMember = new PartyMember
            {
                Index = index
            };

            partyMemberReader.ReadPartyMember(partyMember, dataReader, partyTextReader);

            return partyMember;
        }
    }
}
