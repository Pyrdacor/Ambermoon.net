using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Characters
{
    public class PartyMemberReader : CharacterReader, IPartyMemberReader
    {
        public void ReadPartyMember(PartyMember partyMember,
            IDataReader dataReader, IDataReader partyTextReader)
        {
            ReadCharacter(partyMember, dataReader);
            EventReader.ReadEvents(dataReader, partyMember.Events, partyMember.EventList);
            partyMember.Texts = partyTextReader == null ? new List<string>() : TextReader.ReadTexts(partyTextReader);
        }
    }
}
