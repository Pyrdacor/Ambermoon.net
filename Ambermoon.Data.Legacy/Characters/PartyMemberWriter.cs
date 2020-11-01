using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class PartyMemberWriter : CharacterWriter, IPartyMemberWriter
    {
        public void WritePartyMember(PartyMember partyMember, IDataWriter dataWriter)
        {
            WriteCharacter(partyMember, dataWriter);
            EventWriter.WriteEvents(dataWriter, partyMember.Events, partyMember.EventList);            
        }

        public void WritePartyMember(PartyMember partyMember, IDataWriter dataWriter, IDataWriter textWriter)
        {
            WritePartyMember(partyMember, dataWriter);
            TextWriter.WriteTexts(textWriter, partyMember.Texts);
        }
    }
}
